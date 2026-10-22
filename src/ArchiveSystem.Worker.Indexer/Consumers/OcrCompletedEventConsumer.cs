// src/ArchiveSystem.Worker.Indexer/Consumers/OcrCompletedEventConsumer.cs
using System.Text.Json;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Events;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Common;
using ArchiveSystem.Infrastructure.Persistence;
using ArchiveSystem.Infrastructure.Services;
using MassTransit;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenSearch.Client;
using OpenSearch.Net;

namespace ArchiveSystem.Worker.Indexer.Consumers
{
    public class OcrCompletedEventConsumer : IConsumer<OcrCompletedEvent>
    {
        private static readonly SemaphoreSlim IndexCreationLock = new(1, 1);
        private static int _dossierSchemaState;
        private readonly ArchiveDbContext _dbContext;
        private readonly IOpenSearchClient _openSearchClient;
        private readonly ILogger<OcrCompletedEventConsumer> _logger;
        private readonly string _indexPrefix;

        public OcrCompletedEventConsumer(
            ArchiveDbContext dbContext,
            IOpenSearchClient openSearchClient,
            IConfiguration configuration,
            ILogger<OcrCompletedEventConsumer> logger)
        {
            _dbContext = dbContext;
            _openSearchClient = openSearchClient;
            _logger = logger;
            _indexPrefix = configuration.GetValue<string>("OpenSearch:IndexPrefix") ?? "files";
        }

        public async Task Consume(ConsumeContext<OcrCompletedEvent> context)
        {
            var message = context.Message;
            await IndexFileVersionAsync(message.FileId, message.FileVersionId, context.CancellationToken);
        }

        public async Task IndexFileVersionAsync(
            long fileId,
            long fileVersionId,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Indexing OCR content for file ID: {FileId}", fileId);

            try
            {
                var file = await _dbContext.Files
                    .AsNoTracking()
                    .Include(f => f.Case)
                    .Include(f => f.FileTags)
                        .ThenInclude(ft => ft.Tag)
                    .FirstOrDefaultAsync(f => f.Id == fileId, cancellationToken);
                if (file == null)
                {
                    _logger.LogWarning("File not found in database: {FileId}", fileId);
                    return;
                }

                var version = await _dbContext.FileVersions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Id == fileVersionId, cancellationToken);
                if (version == null || version.FileId != fileId || string.IsNullOrWhiteSpace(version.OcrResult))
                {
                    _logger.LogWarning("OCR result not available for file version {FileVersionId}", fileVersionId);
                    return;
                }

                var ocrContent = ExtractText(version.OcrResult);

                Dossier? dossier = null;
                if (Volatile.Read(ref _dossierSchemaState) >= 0)
                {
                    try
                    {
                        dossier = await _dbContext.Dossiers
                            .AsNoTracking()
                            .Include(d => d.Owners.Where(o => o.DeletedAt == null))
                                .ThenInclude(o => o.Owner)
                            .FirstOrDefaultAsync(
                                d => d.DossierNumber == (file.Case != null ? file.Case.CaseNumber : string.Empty),
                                cancellationToken);
                        Volatile.Write(ref _dossierSchemaState, 1);
                    }
                    catch (SqlException ex) when (ex.Number == 208)
                    {
                        if (Interlocked.Exchange(ref _dossierSchemaState, -1) != -1)
                        {
                            _logger.LogWarning(
                                "The Dossiers table is not available. OpenSearch indexing will use legacy Case metadata.");
                        }
                    }
                }

                var ownerDocuments = new List<FileIndexOwner>();
                if (dossier != null)
                {
                    ownerDocuments = dossier.Owners
                        .Select(link => MapOwner(link.Owner))
                        .Where(owner => !string.IsNullOrWhiteSpace(owner.FirstName) || !string.IsNullOrWhiteSpace(owner.LastName))
                        .ToList();
                }
                else if (!string.IsNullOrWhiteSpace(file.Case?.Owners))
                {
                    ownerDocuments = ParseLegacyCaseOwners(file.Case.Owners);
                }

                var tags = file.FileTags
                    .Select(ft => ft.Tag?.Name?.Trim())
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (tags.Count == 0 && !string.IsNullOrWhiteSpace(file.Tags))
                {
                    tags = ParseLegacyTags(file.Tags);
                }

                var indexDocument = new FileIndexDocument
                {
                    Id = file.Id.ToString(),
                    DossierNumber = dossier?.DossierNumber ?? file.Case?.CaseNumber ?? string.Empty,
                    PostalCode = dossier?.PostalCode ?? string.Empty,
                    FileName = file.Name,
                    CreatedAt = file.CreatedAt,
                    CreatedByUserId = file.Case?.CreatedByUserId,
                    CreatedByUserName = file.Case?.CreatedByUserName ?? string.Empty,
                    OcrText = PersianTextNormalizer.NormalizeForSearch(ocrContent),
                    Tags = tags,
                    Owners = ownerDocuments
                };

                // Index the document in OpenSearch with monthly index strategy
                var indexName = $"{_indexPrefix}-{DateTime.UtcNow:yyyy-MM}";
                await EnsureIndexExistsAsync(indexName, cancellationToken);
                
                var response = await _openSearchClient.IndexAsync(indexDocument, idx => idx
                    .Index(indexName)
                    .Id(file.Id.ToString())
                    .Refresh(Refresh.WaitFor), cancellationToken);

                if (!response.IsValid)
                {
                    _logger.LogError("Failed to index file {FileId} in OpenSearch: {Error}", 
                        file.Id, response.DebugInformation);
                    throw new InvalidOperationException($"Failed to index file {file.Id}: {response.DebugInformation}");
                }

                _logger.LogInformation("Successfully indexed file {FileId} with OCR content to index {IndexName}", 
                    file.Id, indexName);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error indexing OCR content for file ID: {FileId}", fileId);
                throw; // Re-throw to trigger retry mechanism
            }
        }

        private async Task EnsureIndexExistsAsync(string indexName, CancellationToken cancellationToken)
        {
            var existsResponse = await _openSearchClient.Indices.ExistsAsync(indexName, ct: cancellationToken);
            if (existsResponse.Exists)
            {
                return;
            }

            await IndexCreationLock.WaitAsync(cancellationToken);
            try
            {
                existsResponse = await _openSearchClient.Indices.ExistsAsync(indexName, ct: cancellationToken);
                if (existsResponse.Exists)
                {
                    return;
                }

                var createResponse = await _openSearchClient.Indices.CreateAsync(
                    indexName,
                    create => create
                        .Settings(settings => settings
                            .NumberOfShards(1)
                            .NumberOfReplicas(0))
                        .Map<FileIndexDocument>(mapping => mapping
                            .AutoMap()
                            .Properties(properties => properties
                                .Text(text => text
                                    .Name(document => document.OcrText)
                                    .Analyzer("persian")))),
                    cancellationToken);

                if (!createResponse.IsValid)
                {
                    throw new InvalidOperationException(
                        $"Failed to create OpenSearch index '{indexName}': {createResponse.DebugInformation}");
                }

                _logger.LogInformation("Created OpenSearch index {IndexName} with the archive file mapping", indexName);
            }
            finally
            {
                IndexCreationLock.Release();
            }
        }

        private static string ExtractText(string ocrJson)
        {
            try
            {
                using var document = JsonDocument.Parse(ocrJson);
                if (document.RootElement.TryGetProperty("text", out var textNode) && textNode.ValueKind == JsonValueKind.String)
                {
                    return textNode.GetString() ?? string.Empty;
                }
            }
            catch
            {
                // Ignore malformed OCR JSON and index empty content.
            }

            return string.Empty;
        }

        private static FileIndexOwner MapOwner(ArchiveSystem.Domain.Entities.Owner? owner)
        {
            if (owner == null)
            {
                return new FileIndexOwner();
            }

            if (owner.OwnerType == OwnerType.Natural)
            {
                return new FileIndexOwner
                {
                    FirstName = owner.FirstName ?? string.Empty,
                    LastName = owner.LastName ?? string.Empty,
                };
            }

            return new FileIndexOwner
            {
                FirstName = owner.CompanyName ?? string.Empty,
                LastName = string.Empty,
            };
        }

        private static List<FileIndexOwner> ParseLegacyCaseOwners(string rawOwnersJson)
        {
            var owners = new List<FileIndexOwner>();
            try
            {
                using var document = JsonDocument.Parse(rawOwnersJson);
                if (document.RootElement.ValueKind != JsonValueKind.Array)
                {
                    return owners;
                }

                foreach (var item in document.RootElement.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        owners.Add(new FileIndexOwner { FirstName = item.GetString() ?? string.Empty, LastName = string.Empty });
                        continue;
                    }

                    if (item.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    var firstName = item.TryGetProperty("firstName", out var firstNameNode) && firstNameNode.ValueKind == JsonValueKind.String
                        ? firstNameNode.GetString() ?? string.Empty
                        : string.Empty;

                    var lastName = item.TryGetProperty("lastName", out var lastNameNode) && lastNameNode.ValueKind == JsonValueKind.String
                        ? lastNameNode.GetString() ?? string.Empty
                        : string.Empty;

                    if (string.IsNullOrWhiteSpace(firstName) && item.TryGetProperty("fullName", out var fullNameNode) && fullNameNode.ValueKind == JsonValueKind.String)
                    {
                        firstName = fullNameNode.GetString() ?? string.Empty;
                    }

                    owners.Add(new FileIndexOwner
                    {
                        FirstName = firstName,
                        LastName = lastName,
                    });
                }
            }
            catch
            {
                // Ignore legacy parsing failures.
            }

            return owners;
        }

        private static List<string> ParseLegacyTags(string tagsJson)
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(tagsJson);
                return parsed?
                    .Where(tag => !string.IsNullOrWhiteSpace(tag))
                    .Select(tag => tag.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList() ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
