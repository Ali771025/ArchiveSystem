// src/ArchiveSystem.Infrastructure/Services/OpenSearchService.cs (Updated version)
using OpenSearch.Client;
using Microsoft.Extensions.Logging;
using ArchiveSystem.Domain.Services;
using ArchiveSystem.Application.Common.Models;
using ArchiveSystem.Application.Common.Models.AdvancedSearch;
using ArchiveSystem.Application.Interfaces;
using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ArchiveSystem.Domain.Common;

namespace ArchiveSystem.Infrastructure.Services
{
    public class OpenSearchService : ISearchService, IAdvancedFileSearchService
    {
        private readonly IOpenSearchClient _client;
        private readonly ILogger<OpenSearchService> _logger;
        private readonly ArchiveDbContext _dbContext;
        private readonly string _indexPrefix;

        public OpenSearchService(
            IOpenSearchClient client,
            ILogger<OpenSearchService> logger,
            IConfiguration configuration,
            ArchiveDbContext dbContext)
        {
            _client = client;
            _logger = logger;
            _dbContext = dbContext;
            _indexPrefix = configuration.GetValue<string>("OpenSearch:IndexPrefix") ?? "files";
        }

        public async Task IndexFileAsync(long fileId, string content, CancellationToken cancellationToken = default)
        {
            try
            {
                var indexName = GetIndexName(DateTime.UtcNow);
                
                var document = new IndexedFileDocument
                {
                    Id = fileId,
                    Content = content,
                    CreatedAt = DateTime.UtcNow
                };

                var response = await _client.IndexAsync(document, idx => idx
                    .Index(indexName)
                    .Id(fileId.ToString()),
                    cancellationToken);

                if (!response.IsValid)
                {
                    _logger.LogError("Failed to index file {FileId}: {Error}", fileId, response.DebugInformation);
                    throw new InvalidOperationException($"Failed to index file {fileId}: {response.DebugInformation}");
                }

                _logger.LogInformation("Successfully indexed file {FileId} to index {IndexName}", fileId, indexName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error indexing file {FileId}", fileId);
                throw;
            }
        }

        public async Task UpdateFileIndexAsync(long fileId, string content, CancellationToken cancellationToken = default)
        {
            try
            {
                var indexName = GetIndexName(DateTime.UtcNow);
                
                var response = await _client.UpdateAsync<IndexedFileDocument>(fileId.ToString(), u => u
                    .Index(indexName)
                    .Doc(new IndexedFileDocument { Content = content }),
                    cancellationToken);

                if (!response.IsValid)
                {
                    _logger.LogError("Failed to update index for file {FileId}: {Error}", fileId, response.DebugInformation);
                    throw new InvalidOperationException($"Failed to update index for file {fileId}: {response.DebugInformation}");
                }

                _logger.LogInformation("Successfully updated index for file {FileId}", fileId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating index for file {FileId}", fileId);
                throw;
            }
        }

        public async Task DeleteFileIndexAsync(long fileId, CancellationToken cancellationToken = default)
        {
            try
            {
                var indexName = GetIndexName(DateTime.UtcNow);
                
                var response = await _client.DeleteAsync<IndexedFileDocument>(fileId.ToString(), d => d
                    .Index(indexName),
                    cancellationToken);

                if (!response.IsValid)
                {
                    _logger.LogError("Failed to delete index for file {FileId}: {Error}", fileId, response.DebugInformation);
                }
                else
                {
                    _logger.LogInformation("Successfully deleted index for file {FileId}", fileId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting index for file {FileId}", fileId);
                throw;
            }
        }

        public async Task<IReadOnlyList<(long FileId, float Score)>> SearchAsync(string query, int limit = 10, CancellationToken cancellationToken = default)
        {
            try
            {
                var indices = GetIndicesForSearch();
                
                var searchResponse = await _client.SearchAsync<IndexedFileDocument>(s => s
                    .Index(indices)
                    .Query(q => q
                        .MultiMatch(m => m
                            .Fields(f => f.Field(p => p.Content).Field(p => p.Name))
                            .Query(query)
                            .Type(TextQueryType.BestFields)
                            .Fuzziness(Fuzziness.Auto)
                        )
                    )
                    .Size(limit)
                    .Sort(ss => ss.Descending(f => f.CreatedAt)),
                    cancellationToken);

                if (!searchResponse.IsValid)
                {
                    _logger.LogError("Search failed: {Error}", searchResponse.DebugInformation);
                    return new List<(long FileId, float Score)>();
                }

                var results = searchResponse.Documents
                    .Select(doc => (doc.Id, (float)doc.Score))
                    .ToList();

                _logger.LogInformation("Search completed with {Count} results for query '{Query}'", results.Count, query);

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for query '{Query}'", query);
                throw;
            }
        }

        public async Task<IReadOnlyList<(long FileId, float Score)>> SearchByMetadataAsync(IDictionary<string, string> metadata, int limit = 10, CancellationToken cancellationToken = default)
        {
            try
            {
                var indices = GetIndicesForSearch();
                
                var queryContainer = new BoolQueryDescriptor<IndexedFileDocument>();
                
                foreach (var kvp in metadata)
                {
                    queryContainer = queryContainer.Must(q => q
                        .Term(t => t
                            .Field(kvp.Key.ToLower())
                            .Value(kvp.Value)
                        )
                    );
                }

                var searchResponse = await _client.SearchAsync<IndexedFileDocument>(s => s
                    .Index(indices)
                    .Query(q => q.Bool(b => queryContainer))
                    .Size(limit)
                    .Sort(ss => ss.Descending(f => f.CreatedAt)),
                    cancellationToken);

                if (!searchResponse.IsValid)
                {
                    _logger.LogError("Metadata search failed: {Error}", searchResponse.DebugInformation);
                    return new List<(long FileId, float Score)>();
                }

                var results = searchResponse.Documents
                    .Select(doc => (doc.Id, (float)doc.Score))
                    .ToList();

                _logger.LogInformation("Metadata search completed with {Count} results", results.Count);

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching by metadata");
                throw;
            }
        }

        public async Task<PagedSearchResultDto<SearchResultItemDto>> AdvancedSearchAsync(
            AdvancedSearchQueryDto query,
            CancellationToken cancellationToken = default)
        {
            var normalizedTags = query.Tags?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            var mustQueries = new List<QueryContainer>();
            var queryDescriptor = new QueryContainerDescriptor<FileIndexDocument>();

            if (!string.IsNullOrWhiteSpace(query.GeneralSearchTerm))
            {
                var term = PersianTextNormalizer.NormalizeForSearch(query.GeneralSearchTerm);
                mustQueries.Add(
                    queryDescriptor.MultiMatch(mm => mm
                        .Query(term)
                        .Fields(fields => fields
                            .Field(f => f.FileName)
                            .Field(f => f.OcrText))
                        .Type(TextQueryType.Phrase)
                        .Boost(2)));
            }

            if (!string.IsNullOrWhiteSpace(query.DossierNumber))
            {
                mustQueries.Add(
                    queryDescriptor.Term(t => t
                        .Field(f => f.DossierNumber)
                        .Value(query.DossierNumber.Trim())));
            }

            if (!string.IsNullOrWhiteSpace(query.PostalCode))
            {
                mustQueries.Add(
                    queryDescriptor.Term(t => t
                        .Field(f => f.PostalCode)
                        .Value(query.PostalCode.Trim())));
            }

            if (!string.IsNullOrWhiteSpace(query.CreatedBy))
            {
                var createdBy = query.CreatedBy.Trim();
                mustQueries.Add(long.TryParse(createdBy, out var createdByUserId)
                    ? queryDescriptor.Term(term => term
                        .Field(document => document.CreatedByUserId)
                        .Value(createdByUserId))
                    : queryDescriptor.Term(term => term
                        .Field(document => document.CreatedByUserName)
                        .Value(createdBy)));
            }

            if (normalizedTags.Count > 0)
            {
                if (query.TagMode == TagSearchMode.All)
                {
                    var allTagMust = normalizedTags
                        .Select<string, Func<QueryContainerDescriptor<FileIndexDocument>, QueryContainer>>(tag =>
                            q => q.Term(t => t.Field(f => f.Tags).Value(tag)))
                        .ToArray();

                    mustQueries.Add(
                        queryDescriptor.Bool(b => b.Must(allTagMust)));
                }
                else
                {
                    mustQueries.Add(
                        queryDescriptor.Terms(t => t
                            .Field(f => f.Tags)
                            .Terms(normalizedTags)));
                }
            }

            if (query.FromDate.HasValue || query.ToDate.HasValue)
            {
                mustQueries.Add(
                    queryDescriptor.DateRange(range =>
                    {
                        var descriptor = range.Field(f => f.CreatedAt);
                        if (query.FromDate.HasValue)
                        {
                            descriptor = descriptor.GreaterThanOrEquals(query.FromDate.Value.Date);
                        }

                        if (query.ToDate.HasValue)
                        {
                            descriptor = descriptor.LessThan(query.ToDate.Value.Date.AddDays(1));
                        }

                        return descriptor;
                    }));
            }

            if (!string.IsNullOrWhiteSpace(query.OwnerFirstName) || !string.IsNullOrWhiteSpace(query.OwnerLastName))
            {
                var ownerFilters = new List<Func<QueryContainerDescriptor<FileIndexDocument>, QueryContainer>>();
                if (!string.IsNullOrWhiteSpace(query.OwnerFirstName))
                {
                    ownerFilters.Add(of => of.Match(mt => mt
                        .Field("owners.firstName")
                        .Query(query.OwnerFirstName!.Trim())));
                }

                if (!string.IsNullOrWhiteSpace(query.OwnerLastName))
                {
                    ownerFilters.Add(of => of.Match(mt => mt
                        .Field("owners.lastName")
                        .Query(query.OwnerLastName!.Trim())));
                }

                mustQueries.Add(
                    queryDescriptor.Nested(n => n
                        .Path(p => p.Owners)
                        .Query(nq => nq.Bool(nb => nb.Must(ownerFilters.ToArray())))));
            }

            var from = (query.PageNumber - 1) * query.PageSize;

            var response = await _client.SearchAsync<FileIndexDocument>(s => s
                .Index(GetIndicesForSearch())
                .IgnoreUnavailable()
                .AllowNoIndices()
                .From(from)
                .Size(query.PageSize)
                .Query(q => q.Bool(b =>
                {
                    if (mustQueries.Count > 0)
                    {
                        b.Must(mustQueries
                            .Select(queryContainer => (Func<QueryContainerDescriptor<FileIndexDocument>, QueryContainer>)(_ => queryContainer))
                            .ToArray());
                    }

                    return b;
                }))
                .Sort(sort => ApplyAdvancedSort(sort, query))
                .Source(source => source.Includes(i => i
                    .Field(f => f.Id)
                    .Field(f => f.DossierNumber)
                    .Field(f => f.FileName)
                    .Field(f => f.CreatedAt)
                    .Field(f => f.CreatedByUserName)))
                .Highlight(h => h
                    .Fields(
                        hf => hf
                            .Field(f => f.OcrText)
                            .FragmentSize(140)
                            .NumberOfFragments(2)
                            .PreTags("<mark>")
                            .PostTags("</mark>")))
                .Aggregations(aggs => aggs
                    .Terms("tags", t => t
                        .Field(f => f.Tags)
                        .Size(20))
                    .Terms("dossiers", t => t
                        .Field(f => f.DossierNumber)
                        .Size(20))),
                cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogWarning("Advanced OpenSearch query failed. Falling back to database search. {DebugInformation}", response.DebugInformation);
                return await SearchAdvancedFromDatabaseAsync(query, cancellationToken);
            }

            var items = response.Hits.Select(hit =>
            {
                var id = Guid.TryParse(hit.Source?.Id, out var parsedGuid) ? parsedGuid : Guid.Empty;
                var highlights = ExtractHighlights(hit);

                return new SearchResultItemDto
                {
                    Id = hit.Source?.Id ?? id.ToString(),
                    DossierNumber = hit.Source?.DossierNumber ?? string.Empty,
                    FileName = hit.Source?.FileName ?? string.Empty,
                    CreatedAt = hit.Source?.CreatedAt,
                    CreatedBy = hit.Source?.CreatedByUserName,
                    Highlights = highlights
                };
            }).ToList();

            if (items.Count == 0)
            {
                return await SearchAdvancedFromDatabaseAsync(query, cancellationToken);
            }

            return new PagedSearchResultDto<SearchResultItemDto>
            {
                Items = items,
                TotalHits = response.Total,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                Aggregations = new AdvancedSearchAggregationsDto
                {
                    Tags = ReadTermsAggregation(response, "tags"),
                    Dossiers = ReadTermsAggregation(response, "dossiers"),
                }
            };
        }

        private async Task<PagedSearchResultDto<SearchResultItemDto>> SearchAdvancedFromDatabaseAsync(
            AdvancedSearchQueryDto query,
            CancellationToken cancellationToken)
        {
            var normalizedPage = query.PageNumber < 1 ? 1 : query.PageNumber;
            var normalizedPageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, 100);
            var normalizedTerm = PersianTextNormalizer.NormalizeForSearch(query.GeneralSearchTerm);
            var cases = _dbContext.Cases
                .AsNoTracking()
                .Where(c => c.DeletedAt == null);

            if (!string.IsNullOrWhiteSpace(query.DossierNumber))
            {
                var dossierNumber = query.DossierNumber.Trim();
                cases = cases.Where(c => EF.Functions.Like(c.CaseNumber, $"%{dossierNumber}%"));
            }

            if (!string.IsNullOrWhiteSpace(query.PostalCode))
            {
                var postalCode = query.PostalCode.Trim();
                cases = cases.Where(c => c.PostalCode != null && EF.Functions.Like(c.PostalCode, $"%{postalCode}%"));
            }

            if (!string.IsNullOrWhiteSpace(query.CreatedBy))
            {
                var createdBy = query.CreatedBy.Trim();
                if (long.TryParse(createdBy, out var createdByUserId))
                {
                    cases = cases.Where(c => c.CreatedByUserId == createdByUserId);
                }
                else
                {
                    cases = cases.Where(c => c.CreatedByUserName != null && EF.Functions.Like(c.CreatedByUserName, $"%{createdBy}%"));
                }
            }

            var candidateCases = await cases
                .Select(c => new
                {
                    c.Id,
                    c.CaseNumber,
                    c.Title,
                    c.Location,
                    c.Owners,
                    c.CreatedByUserName,
                    c.CreatedAt,
                    c.ModifiedAt
                })
                .ToListAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(query.OwnerFirstName) || !string.IsNullOrWhiteSpace(query.OwnerLastName))
            {
                var ownerFirstName = query.OwnerFirstName?.Trim() ?? string.Empty;
                var ownerLastName = query.OwnerLastName?.Trim() ?? string.Empty;
                candidateCases = candidateCases
                    .Where(c =>
                    {
                        var names = ArchiveSystem.Application.Common.OwnerDisplayNameParser.ParseNames(c.Owners);
                        return names.Any(name =>
                            (string.IsNullOrWhiteSpace(ownerFirstName) || name.Contains(ownerFirstName, StringComparison.OrdinalIgnoreCase)) &&
                            (string.IsNullOrWhiteSpace(ownerLastName) || name.Contains(ownerLastName, StringComparison.OrdinalIgnoreCase)));
                    })
                    .ToList();
            }

            var candidateCaseIds = candidateCases.Select(c => c.Id).ToArray();
            var filesQuery = _dbContext.Files
                .AsNoTracking()
                .Where(f =>
                    f.CaseId.HasValue &&
                    candidateCaseIds.Contains(f.CaseId.Value) &&
                    f.DeletedAt == null);

            if (query.FromDate.HasValue)
            {
                filesQuery = filesQuery.Where(
                    file => file.CreatedAt >= query.FromDate.Value.Date);
            }

            if (query.ToDate.HasValue)
            {
                var exclusiveTo = query.ToDate.Value.Date.AddDays(1);
                filesQuery = filesQuery.Where(file => file.CreatedAt < exclusiveTo);
            }

            var files = await filesQuery
                .Select(f => new
                {
                    f.Id,
                    f.Name,
                    f.CaseId,
                    f.CreatedAt,
                    OcrText = f.LatestVersionId.HasValue
                        ? _dbContext.FileVersions
                            .Where(version => version.Id == f.LatestVersionId.Value)
                            .Select(version => version.OcrText)
                            .FirstOrDefault()
                        : null
                })
                .ToListAsync(cancellationToken);

            var caseMatches = candidateCases
                .Where(caseItem =>
                    string.IsNullOrEmpty(normalizedTerm) ||
                    PersianTextNormalizer.NormalizeForSearch(
                        $"{caseItem.CaseNumber} {caseItem.Title} {caseItem.Location}")
                        .Contains(normalizedTerm, StringComparison.Ordinal))
                .Select(caseItem => caseItem.Id)
                .ToHashSet();

            if (!string.IsNullOrEmpty(normalizedTerm))
            {
                files = files
                    .Where(file =>
                        PersianTextNormalizer.NormalizeForSearch(file.Name)
                            .Contains(normalizedTerm, StringComparison.Ordinal) ||
                        PersianTextNormalizer.NormalizeForSearch(file.OcrText)
                            .Contains(normalizedTerm, StringComparison.Ordinal))
                    .ToList();
            }

            var rows = new List<AdvancedSearchDatabaseRow>();
            foreach (var caseItem in candidateCases)
            {
                var caseFiles = files.Where(f => f.CaseId == caseItem.Id).ToList();
                if (caseFiles.Count == 0)
                {
                    if (query.FromDate.HasValue || query.ToDate.HasValue ||
                        (!string.IsNullOrEmpty(normalizedTerm) && !caseMatches.Contains(caseItem.Id)))
                    {
                        continue;
                    }

                    rows.Add(new AdvancedSearchDatabaseRow(
                        new SearchResultItemDto
                        {
                            Id = $"case-{caseItem.Id}",
                            DossierNumber = caseItem.CaseNumber,
                            FileName = caseItem.Title,
                            CreatedAt = caseItem.ModifiedAt ?? caseItem.CreatedAt,
                            CreatedBy = caseItem.CreatedByUserName,
                            Highlights = new List<string> { caseItem.Location }
                        },
                        caseItem.ModifiedAt ?? caseItem.CreatedAt));
                    continue;
                }

                rows.AddRange(caseFiles.Select(file =>
                {
                    var ocrSnippet = BuildHighlightedSnippet(file.OcrText, normalizedTerm);
                    return new AdvancedSearchDatabaseRow(
                        new SearchResultItemDto
                        {
                            Id = file.Id.ToString(),
                            DossierNumber = caseItem.CaseNumber,
                            FileName = file.Name,
                            CreatedAt = file.CreatedAt,
                            CreatedBy = caseItem.CreatedByUserName,
                            Highlights = !string.IsNullOrWhiteSpace(ocrSnippet)
                                ? new List<string> { ocrSnippet }
                                : new List<string> { caseItem.Title }
                        },
                        file.CreatedAt);
                }));
            }

            rows = ApplyDatabaseAdvancedSort(rows, query);

            var totalHits = rows.Count;
            var pageItems = rows
                .Skip((normalizedPage - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .Select(row => row.Item)
                .ToList();

            return new PagedSearchResultDto<SearchResultItemDto>
            {
                Items = pageItems,
                TotalHits = totalHits,
                PageNumber = normalizedPage,
                PageSize = normalizedPageSize,
                Aggregations = new AdvancedSearchAggregationsDto()
            };
        }

        private static string? BuildHighlightedSnippet(string? text, string normalizedTerm)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(normalizedTerm))
            {
                return null;
            }

            var normalizedText = PersianTextNormalizer.NormalizeForSearch(text);
            var matchIndex = normalizedText.IndexOf(normalizedTerm, StringComparison.Ordinal);
            if (matchIndex < 0)
            {
                return null;
            }

            const int contextLength = 90;
            var start = Math.Max(0, matchIndex - contextLength);
            var end = Math.Min(normalizedText.Length, matchIndex + normalizedTerm.Length + contextLength);
            var before = System.Net.WebUtility.HtmlEncode(normalizedText[start..matchIndex]);
            var match = System.Net.WebUtility.HtmlEncode(
                normalizedText.Substring(matchIndex, normalizedTerm.Length));
            var after = System.Net.WebUtility.HtmlEncode(
                normalizedText[(matchIndex + normalizedTerm.Length)..end]);

            return $"{(start > 0 ? "…" : string.Empty)}{before}<mark>{match}</mark>{after}{(end < normalizedText.Length ? "…" : string.Empty)}";
        }

        private static List<AdvancedSearchDatabaseRow> ApplyDatabaseAdvancedSort(
            IEnumerable<AdvancedSearchDatabaseRow> rows,
            AdvancedSearchQueryDto query)
        {
            var isAscending = query.SortDirection == SortDirection.Asc;
            return query.SortBy switch
            {
                AdvancedSearchSortBy.Date => isAscending
                    ? rows.OrderBy(row => row.SortDate).ThenBy(row => row.Item.FileName).ToList()
                    : rows.OrderByDescending(row => row.SortDate).ThenBy(row => row.Item.FileName).ToList(),
                AdvancedSearchSortBy.FileName => isAscending
                    ? rows.OrderBy(row => row.Item.FileName).ThenByDescending(row => row.SortDate).ToList()
                    : rows.OrderByDescending(row => row.Item.FileName).ThenByDescending(row => row.SortDate).ToList(),
                _ => rows.OrderByDescending(row => row.SortDate).ThenBy(row => row.Item.FileName).ToList()
            };
        }

        private static List<string> ExtractHighlights(IHit<FileIndexDocument> hit)
        {
            var highlightsObject =
                hit.GetType().GetProperty("Highlights")?.GetValue(hit) ??
                hit.GetType().GetProperty("Highlight")?.GetValue(hit);

            if (highlightsObject is null)
            {
                return new List<string>();
            }

            var fragments = new List<string>();
            if (highlightsObject is System.Collections.IDictionary dictionary)
            {
                foreach (var value in dictionary.Values)
                {
                    AppendFragments(value, fragments);
                }
            }

            return fragments
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        private static void AppendFragments(object? highlightValue, ICollection<string> fragments)
        {
            if (highlightValue is null)
            {
                return;
            }

            var valueType = highlightValue.GetType();
            var valuesObject =
                valueType.GetProperty("Highlights")?.GetValue(highlightValue) ??
                valueType.GetProperty("Fragments")?.GetValue(highlightValue);

            if (valuesObject is IEnumerable<string> stringValues)
            {
                foreach (var fragment in stringValues)
                {
                    if (!string.IsNullOrWhiteSpace(fragment))
                    {
                        fragments.Add(fragment);
                    }
                }
            }
        }

        private static SortDescriptor<FileIndexDocument> ApplyAdvancedSort(
            SortDescriptor<FileIndexDocument> sort,
            AdvancedSearchQueryDto query)
        {
            if (query.SortBy == AdvancedSearchSortBy.Relevance)
            {
                return sort;
            }

            if (query.SortBy == AdvancedSearchSortBy.Date)
            {
                return query.SortDirection == SortDirection.Asc
                    ? sort.Ascending(x => x.CreatedAt)
                    : sort.Descending(x => x.CreatedAt);
            }

            return query.SortDirection == SortDirection.Asc
                ? sort.Ascending("fileName.keyword")
                : sort.Descending("fileName.keyword");
        }

        private static Dictionary<string, long> ReadTermsAggregation(
            ISearchResponse<FileIndexDocument> response,
            string aggregationName)
        {
            var result = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            var aggregation = response.Aggregations.Terms(aggregationName);
            if (aggregation?.Buckets == null)
            {
                return result;
            }

            foreach (var bucket in aggregation.Buckets)
            {
                if (string.IsNullOrWhiteSpace(bucket.Key))
                {
                    continue;
                }

                result[bucket.Key] = bucket.DocCount ?? 0;
            }

            return result;
        }

        private sealed record AdvancedSearchDatabaseRow(SearchResultItemDto Item, DateTime SortDate);

        private string GetIndexName(DateTime date)
        {
            return $"{_indexPrefix}-{date:yyyy-MM}";
        }

        private Indices GetIndicesForSearch()
        {
            // Search across the current month and previous few months
            var currentDate = DateTime.UtcNow;
            var indices = new List<string>();
            
            for (int i = 0; i < 6; i++) // Search last 6 months
            {
                var date = currentDate.AddMonths(-i);
                indices.Add(GetIndexName(date));
            }

            return Indices.Parse(string.Join(",", indices));
        }
    }

    public class IndexedFileDocument
    {
        [Keyword(Name = "id")]
        public long Id { get; set; }

        [Text(Name = "content")]
        public string Content { get; set; } = string.Empty;

        [Text(Name = "name")]
        public string Name { get; set; } = string.Empty;

        [Date(Name = "created_at")]
        public DateTime CreatedAt { get; set; }

        [Number(NumberType.Integer, Name = "size")]
        public long Size { get; set; }

        [Keyword(Name = "extension")]
        public string Extension { get; set; } = string.Empty;

        [Keyword(Name = "classification_id")]
        public long? ClassificationId { get; set; }

        [Number(NumberType.Float, Name = "score")]
        public float Score { get; set; }
    }

    public sealed class FileIndexDocument
    {
        [Keyword(Name = "id")]
        public string Id { get; set; } = string.Empty;

        [Keyword(Name = "dossierNumber")]
        public string DossierNumber { get; set; } = string.Empty;

        [Keyword(Name = "postalCode")]
        public string PostalCode { get; set; } = string.Empty;

        [Text(Name = "fileName")]
        public string FileName { get; set; } = string.Empty;

        [Date(Name = "createdAt")]
        public DateTime CreatedAt { get; set; }

        [Number(NumberType.Long, Name = "createdByUserId")]
        public long? CreatedByUserId { get; set; }

        [Keyword(Name = "createdByUserName")]
        public string CreatedByUserName { get; set; } = string.Empty;

        [Text(Name = "ocrText")]
        public string OcrText { get; set; } = string.Empty;

        [Keyword(Name = "tags")]
        public List<string> Tags { get; set; } = new();

        [Nested(Name = "owners")]
        public List<FileIndexOwner> Owners { get; set; } = new();
    }

    public sealed class FileIndexOwner
    {
        [Text(Name = "firstName")]
        public string FirstName { get; set; } = string.Empty;

        [Text(Name = "lastName")]
        public string LastName { get; set; } = string.Empty;
    }
}
