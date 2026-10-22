using System.Data;
using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using ArchiveSystem.Application.Interfaces.Storage;
using ArchiveSystem.Application.Options;
using ArchiveSystem.Infrastructure.Persistence;
using ArchiveSystem.Presentation.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ArchiveSystem.Presentation.Api.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    [ApiController]
    [Route("api/v1/backups")]
    public sealed class BackupsController : ControllerBase
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

        private readonly ArchiveDbContext _dbContext;
        private readonly IObjectStorage _objectStorage;
        private readonly ObjectStorageOptions _objectStorageOptions;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<BackupsController> _logger;

        public BackupsController(
            ArchiveDbContext dbContext,
            IObjectStorage objectStorage,
            IOptions<ObjectStorageOptions> objectStorageOptions,
            IWebHostEnvironment environment,
            ILogger<BackupsController> logger)
        {
            _dbContext = dbContext;
            _objectStorage = objectStorage;
            _objectStorageOptions = objectStorageOptions.Value;
            _environment = environment;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResult<IReadOnlyCollection<BackupFileDto>>), StatusCodes.Status200OK)]
        public ActionResult<ApiResult<IReadOnlyCollection<BackupFileDto>>> GetBackups()
        {
            var backups = Directory.Exists(BackupDirectory)
                ? Directory.EnumerateFiles(BackupDirectory, "*.zip", SearchOption.TopDirectoryOnly)
                    .Select(BuildBackupFileDto)
                    .OrderByDescending(x => x.CreatedAtUtc)
                    .ToArray()
                : Array.Empty<BackupFileDto>();

            return Ok(ApiResult<IReadOnlyCollection<BackupFileDto>>.Ok(backups));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResult<BackupFileDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResult<BackupFileDto>>> CreateBackup(
            [FromBody] CreateBackupRequest? request,
            CancellationToken cancellationToken)
        {
            request ??= new CreateBackupRequest();
            Directory.CreateDirectory(BackupDirectory);

            var startedAtUtc = DateTimeOffset.UtcNow;
            var fileName = $"archive-backup-{startedAtUtc:yyyyMMdd-HHmmss}.zip";
            var outputPath = Path.Combine(BackupDirectory, fileName);
            var actorId = User.FindFirstValue("sub")
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? "unknown";

            var missingObjects = new List<MissingObjectDto>();
            var exportedObjects = 0;

            await using (var output = new FileStream(
                outputPath,
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.None,
                bufferSize: 1024 * 64,
                FileOptions.SequentialScan))
            using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: false))
            {
                var tables = request.IncludeDatabase
                    ? await ExportDatabaseAsync(archive, cancellationToken)
                    : Array.Empty<TableExportDto>();

                IReadOnlyCollection<FileObjectDto> fileObjects = Array.Empty<FileObjectDto>();
                if (request.IncludeFileObjects)
                {
                    fileObjects = await LoadFileObjectsAsync(cancellationToken);
                    foreach (var fileObject in fileObjects.GroupBy(x => x.ObjectKey).Select(x => x.First()))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            var objectEntryName = $"objects/{SanitizeEntryName(fileObject.ObjectKey)}";
                            var entry = archive.CreateEntry(objectEntryName, CompressionLevel.NoCompression);
                            await using var source = await _objectStorage.GetObjectStreamAsync(
                                _objectStorageOptions.BucketName,
                                fileObject.ObjectKey,
                                cancellationToken);
                            await using var target = entry.Open();
                            await source.CopyToAsync(target, 1024 * 64, cancellationToken);
                            exportedObjects++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(
                                ex,
                                "File object could not be added to backup. ObjectKey={ObjectKey}",
                                fileObject.ObjectKey);
                            missingObjects.Add(new MissingObjectDto(fileObject.ObjectKey, ex.Message));
                        }
                    }
                }

                await AddJsonEntryAsync(
                    archive,
                    "storage/file-objects.json",
                    fileObjects,
                    cancellationToken);
                await AddJsonEntryAsync(
                    archive,
                    "storage/missing-objects.json",
                    missingObjects,
                    cancellationToken);

                var manifest = new BackupManifestDto(
                    FileName: fileName,
                    CreatedAtUtc: startedAtUtc,
                    CreatedBy: actorId,
                    Format: "zip: logical-db-json + object-storage-files",
                    DatabaseIncluded: request.IncludeDatabase,
                    FileObjectsIncluded: request.IncludeFileObjects,
                    ExportedTableCount: tables.Count,
                    ExportedObjectCount: exportedObjects,
                    MissingObjectCount: missingObjects.Count,
                    Notes: new[]
                    {
                        "database/*.json contains table-level logical data exports.",
                        "objects/* contains file object bytes copied from configured object storage.",
                        "storage/file-objects.json maps file/version records to object storage keys.",
                        "This backup is stored on the API server under App_Data/Backups and is available for download from the admin panel."
                    });

                await AddJsonEntryAsync(archive, "manifest.json", manifest, cancellationToken);
                await AddTextEntryAsync(archive, "README.txt", BuildReadme(manifest), cancellationToken);
            }

            var dto = BuildBackupFileDto(outputPath);
            return Ok(ApiResult<BackupFileDto>.Ok(dto, "پشتیبان‌گیری با موفقیت انجام شد."));
        }

        [HttpGet("{fileName}")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult DownloadBackup(string fileName)
        {
            if (!IsSafeBackupFileName(fileName))
            {
                return NotFound();
            }

            var path = Path.Combine(BackupDirectory, fileName);
            if (!System.IO.File.Exists(path))
            {
                return NotFound();
            }

            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return File(stream, "application/zip", fileName);
        }

        private string BackupDirectory => Path.Combine(_environment.ContentRootPath, "App_Data", "Backups");

        private async Task<IReadOnlyCollection<TableExportDto>> ExportDatabaseAsync(
            ZipArchive archive,
            CancellationToken cancellationToken)
        {
            var connection = _dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            var tables = await LoadTableNamesAsync(connection, cancellationToken);
            var exported = new List<TableExportDto>();

            foreach (var table in tables)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var rowCount = await ExportTableAsync(archive, connection, table, cancellationToken);
                exported.Add(new TableExportDto(table.Schema, table.Name, rowCount));
            }

            await AddJsonEntryAsync(archive, "database/tables.json", exported, cancellationToken);
            return exported;
        }

        private static async Task<IReadOnlyCollection<TableNameDto>> LoadTableNamesAsync(
            System.Data.Common.DbConnection connection,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT TABLE_SCHEMA, TABLE_NAME
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'BASE TABLE'
                ORDER BY TABLE_SCHEMA, TABLE_NAME
                """;

            var tables = new List<TableNameDto>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                tables.Add(new TableNameDto(reader.GetString(0), reader.GetString(1)));
            }

            return tables;
        }

        private static async Task<long> ExportTableAsync(
            ZipArchive archive,
            System.Data.Common.DbConnection connection,
            TableNameDto table,
            CancellationToken cancellationToken)
        {
            var sqlName = $"{QuoteIdentifier(table.Schema)}.{QuoteIdentifier(table.Name)}";
            await using var countCommand = connection.CreateCommand();
            countCommand.CommandText = $"SELECT COUNT_BIG(1) FROM {sqlName}";
            var rowCount = Convert.ToInt64(await countCommand.ExecuteScalarAsync(cancellationToken));

            await using var exportCommand = connection.CreateCommand();
            exportCommand.CommandText = $"SELECT * FROM {sqlName} FOR JSON PATH, INCLUDE_NULL_VALUES";
            exportCommand.CommandTimeout = 0;

            var entry = archive.CreateEntry($"database/{table.Schema}.{table.Name}.json", CompressionLevel.Optimal);
            await using var entryStream = entry.Open();
            await using var writer = new StreamWriter(entryStream, new UTF8Encoding(false));

            await using var reader = await exportCommand.ExecuteReaderAsync(cancellationToken);
            var wroteAny = false;
            while (await reader.ReadAsync(cancellationToken))
            {
                if (!reader.IsDBNull(0))
                {
                    await writer.WriteAsync(reader.GetString(0));
                    wroteAny = true;
                }
            }

            if (!wroteAny)
            {
                await writer.WriteAsync("[]");
            }

            return rowCount;
        }

        private async Task<IReadOnlyCollection<FileObjectDto>> LoadFileObjectsAsync(CancellationToken cancellationToken)
        {
            var fileVersions = await _dbContext.FileVersions
                .AsNoTracking()
                .Where(x => !x.DeletedAt.HasValue && x.StoragePath != string.Empty)
                .Select(x => new FileObjectDto(
                    x.FileId,
                    x.Id,
                    x.VersionNumber,
                    x.FileName,
                    x.ContentType,
                    x.Size,
                    x.Sha256,
                    x.StoragePath))
                .ToListAsync(cancellationToken);

            if (fileVersions.Count > 0)
            {
                return fileVersions;
            }

            return await _dbContext.Files
                .AsNoTracking()
                .Where(x => !x.DeletedAt.HasValue && x.StoragePath != string.Empty)
                .Select(x => new FileObjectDto(
                    x.Id,
                    null,
                    1,
                    x.Name,
                    x.ContentType,
                    x.Size.Bytes,
                    x.Hash,
                    x.StoragePath))
                .ToListAsync(cancellationToken);
        }

        private static BackupFileDto BuildBackupFileDto(string path)
        {
            var info = new FileInfo(path);
            return new BackupFileDto(
                info.Name,
                info.Length,
                info.CreationTimeUtc,
                info.LastWriteTimeUtc);
        }

        private static async Task AddJsonEntryAsync<T>(
            ZipArchive archive,
            string entryName,
            T value,
            CancellationToken cancellationToken)
        {
            var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            await using var stream = entry.Open();
            await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken);
        }

        private static async Task AddTextEntryAsync(
            ZipArchive archive,
            string entryName,
            string content,
            CancellationToken cancellationToken)
        {
            var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            await using var stream = entry.Open();
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            await writer.WriteAsync(content.AsMemory(), cancellationToken);
        }

        private static string BuildReadme(BackupManifestDto manifest)
        {
            return $"""
                ArchiveSystem backup
                Created at UTC: {manifest.CreatedAtUtc:u}
                Format: {manifest.Format}

                Contents:
                - manifest.json: backup summary
                - database/*.json: logical database table exports
                - storage/file-objects.json: file/version metadata and object keys
                - objects/*: object storage file bytes copied during backup
                - storage/missing-objects.json: objects that could not be copied

                Server storage location: App_Data/Backups
                """;
        }

        private static string QuoteIdentifier(string value)
        {
            return $"[{value.Replace("]", "]]", StringComparison.Ordinal)}]";
        }

        private static string SanitizeEntryName(string value)
        {
            var sanitized = value
                .Replace('\\', '/')
                .Trim('/');

            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                sanitized = sanitized.Replace(invalid, '_');
            }

            return string.IsNullOrWhiteSpace(sanitized) ? Guid.NewGuid().ToString("N") : sanitized;
        }

        private static bool IsSafeBackupFileName(string fileName)
        {
            return fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
                && fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
                && !fileName.Contains("..", StringComparison.Ordinal)
                && !fileName.Contains('/', StringComparison.Ordinal)
                && !fileName.Contains('\\', StringComparison.Ordinal);
        }
    }

    public sealed class CreateBackupRequest
    {
        public bool IncludeDatabase { get; set; } = true;
        public bool IncludeFileObjects { get; set; } = true;
    }

    public sealed record BackupFileDto(
        string FileName,
        long SizeBytes,
        DateTime CreatedAtUtc,
        DateTime LastModifiedAtUtc);

    public sealed record BackupManifestDto(
        string FileName,
        DateTimeOffset CreatedAtUtc,
        string CreatedBy,
        string Format,
        bool DatabaseIncluded,
        bool FileObjectsIncluded,
        int ExportedTableCount,
        int ExportedObjectCount,
        int MissingObjectCount,
        IReadOnlyCollection<string> Notes);

    public sealed record TableNameDto(string Schema, string Name);
    public sealed record TableExportDto(string Schema, string Name, long RowCount);
    public sealed record MissingObjectDto(string ObjectKey, string Error);

    public sealed record FileObjectDto(
        long FileId,
        long? VersionId,
        int VersionNumber,
        string FileName,
        string ContentType,
        long SizeBytes,
        string? Sha256,
        string ObjectKey);
}
