using System.Security.Cryptography;
using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Application.Interfaces.Storage;
using ArchiveSystem.Application.Options;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Events;
using ArchiveSystem.Domain.Services;
using ArchiveSystem.Domain.ValueObjects;
using ArchiveSystem.Worker.ScannerIngest.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FileEntity = ArchiveSystem.Domain.Entities.ArchiveFile;

namespace ArchiveSystem.Worker.ScannerIngest.Workers
{
    public class ScannerIngestWorker : BackgroundService
    {
        private const string HealthMarkerPath = "/tmp/archive-worker/scanner-ingest.started";
        private static readonly HashSet<string> TemporaryExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".tmp", ".part", ".partial", ".crdownload", ".download"
        };

        private readonly IFileRepository _fileRepository;
        private readonly IFileVersionRepository _fileVersionRepository;
        private readonly IObjectStorage _objectStorage;
        private readonly IMessageBus _messageBus;
        private readonly ObjectStorageOptions _storageOptions;
        private readonly ScannerIngestOptions _options;
        private readonly ILogger<ScannerIngestWorker> _logger;
        private readonly Dictionary<string, FileObservation> _observations = new(StringComparer.OrdinalIgnoreCase);

        public ScannerIngestWorker(
            IFileRepository fileRepository,
            IFileVersionRepository fileVersionRepository,
            IObjectStorage objectStorage,
            IMessageBus messageBus,
            IOptions<ObjectStorageOptions> storageOptions,
            IOptions<ScannerIngestOptions> options,
            ILogger<ScannerIngestWorker> logger)
        {
            _fileRepository = fileRepository;
            _fileVersionRepository = fileVersionRepository;
            _objectStorage = objectStorage;
            _messageBus = messageBus;
            _storageOptions = storageOptions.Value;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            TouchHealthMarker();
            if (!_options.Enabled)
            {
                _logger.LogInformation("Scanner ingest is disabled.");
                return;
            }

            EnsureDirectories();
            await _objectStorage.EnsureBucketExistsAsync(_storageOptions.BucketName, stoppingToken);

            if (_options.DefaultClassificationId.HasValue)
            {
                _logger.LogInformation("ScannerIngest.DefaultClassificationId is configured but not used by File entity model: {ClassificationId}", _options.DefaultClassificationId.Value);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                TouchHealthMarker();
                try
                {
                    await ScanAndIngestAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Scanner ingest loop failed");
                }

                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.PollIntervalSeconds)), stoppingToken);
            }
        }

        private static void TouchHealthMarker()
        {
            Directory.CreateDirectory("/tmp/archive-worker");
            System.IO.File.WriteAllText(HealthMarkerPath, DateTimeOffset.UtcNow.ToString("O"));
        }

        private async Task ScanAndIngestAsync(CancellationToken cancellationToken)
        {
            var allowedExtensions = new HashSet<string>(
                (_options.AllowedExtensions ?? Array.Empty<string>())
                    .Select(NormalizeExtension),
                StringComparer.OrdinalIgnoreCase);

            var now = DateTime.UtcNow;
            var discovered = Directory.Exists(_options.WatchPath)
                ? Directory.GetFiles(_options.WatchPath)
                : Array.Empty<string>();

            var existingSet = new HashSet<string>(discovered, StringComparer.OrdinalIgnoreCase);
            var staleKeys = _observations.Keys.Where(path => !existingSet.Contains(path)).ToList();
            foreach (var staleKey in staleKeys)
            {
                _observations.Remove(staleKey);
            }

            foreach (var path in discovered)
            {
                if (!ShouldConsider(path, allowedExtensions))
                {
                    continue;
                }

                var info = new FileInfo(path);
                if (!info.Exists)
                {
                    continue;
                }

                if (!_observations.TryGetValue(path, out var observation))
                {
                    _observations[path] = new FileObservation(info.Length, now);
                    continue;
                }

                if (observation.SizeBytes != info.Length)
                {
                    _observations[path] = new FileObservation(info.Length, now);
                    continue;
                }

                if ((now - observation.UnchangedSinceUtc).TotalSeconds < Math.Max(1, _options.StableSeconds))
                {
                    continue;
                }

                try
                {
                    await IngestFileAsync(path, info, cancellationToken);
                    MoveFile(path, _options.ProcessedPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to ingest scanner file {FilePath}", path);
                    TryMoveToError(path, ex.Message);
                }
                finally
                {
                    _observations.Remove(path);
                }
            }
        }

        private async Task IngestFileAsync(string path, FileInfo fileInfo, CancellationToken cancellationToken)
        {
            var extension = NormalizeExtension(fileInfo.Extension);
            var contentType = ResolveContentType(extension);
            var objectKey = BuildScannerObjectKey(fileInfo.Name);

            await using var readStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var sha256 = await ComputeSha256Async(readStream, cancellationToken);
            readStream.Position = 0;

            await _objectStorage.PutObjectAsync(_storageOptions.BucketName, objectKey, readStream, contentType, cancellationToken);

            var file = new FileEntity(
                id: 0,
                name: fileInfo.Name,
                size: FileSize.Create(fileInfo.Length),
                extension: FileExtension.Create(extension),
                contentType: contentType,
                storagePath: objectKey,
                caseId: _options.DefaultCaseId);

            await _fileRepository.AddAsync(file, cancellationToken);
            await _fileRepository.SaveChangesAsync(cancellationToken);

            var versionNumber = await _fileVersionRepository.GetLatestVersionNumberAsync(file.Id, cancellationToken) + 1;
            var version = new FileVersion(
                id: 0,
                fileId: file.Id,
                versionNumber: versionNumber,
                storagePath: objectKey,
                fileName: fileInfo.Name,
                contentType: contentType,
                size: fileInfo.Length,
                sha256: sha256);

            await _fileVersionRepository.CreateVersionAsync(version, cancellationToken);
            await _fileVersionRepository.SaveChangesAsync(cancellationToken);

            file.ApplyLatestVersionMetadata(version.Id, version.StoragePath, version.Size, version.ContentType, version.Sha256);
            await _fileRepository.UpdateAsync(file, cancellationToken);
            await _fileRepository.SaveChangesAsync(cancellationToken);

            await _messageBus.PublishAsync(
                new FileVersionCreatedEvent(file.Id, version.Id, version.StoragePath, version.ContentType, version.Size),
                cancellationToken);

            _logger.LogInformation("Ingested scanner file {FileName} as file {FileId}, version {VersionId}", fileInfo.Name, file.Id, version.Id);
        }

        private static async Task<string> ComputeSha256Async(Stream source, CancellationToken cancellationToken)
        {
            using var sha = SHA256.Create();
            var hash = await sha.ComputeHashAsync(source, cancellationToken);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private void EnsureDirectories()
        {
            if (!Directory.Exists(_options.WatchPath))
            {
                Directory.CreateDirectory(_options.WatchPath);
            }

            if (!string.IsNullOrWhiteSpace(_options.ProcessedPath))
            {
                Directory.CreateDirectory(_options.ProcessedPath);
            }

            if (!string.IsNullOrWhiteSpace(_options.ErrorPath))
            {
                Directory.CreateDirectory(_options.ErrorPath);
            }
        }

        private bool ShouldConsider(string path, HashSet<string> allowedExtensions)
        {
            var extension = NormalizeExtension(Path.GetExtension(path));
            if (string.IsNullOrWhiteSpace(extension))
            {
                return false;
            }

            if (TemporaryExtensions.Contains(extension))
            {
                return false;
            }

            if (allowedExtensions.Count == 0)
            {
                return true;
            }

            return allowedExtensions.Contains(extension);
        }

        private static string NormalizeExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return string.Empty;
            }

            return extension.StartsWith('.') ? extension.ToLowerInvariant() : $".{extension.ToLowerInvariant()}";
        }

        private static string ResolveContentType(string extension)
        {
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".tif" or ".tiff" => "image/tiff",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }

        private static string BuildScannerObjectKey(string fileName)
        {
            var safeName = Path.GetFileName(fileName);
            return $"scanner/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}_{safeName}";
        }

        private void MoveFile(string sourcePath, string targetDirectory)
        {
            if (string.IsNullOrWhiteSpace(targetDirectory))
            {
                System.IO.File.Delete(sourcePath);
                return;
            }

            var destination = BuildUniqueDestinationPath(targetDirectory, Path.GetFileName(sourcePath));
            System.IO.File.Move(sourcePath, destination);
        }

        private void TryMoveToError(string sourcePath, string error)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_options.ErrorPath))
                {
                    _logger.LogWarning("Scanner ingest error with no ErrorPath configured for file {FilePath}: {Error}", sourcePath, error);
                    return;
                }

                var destination = BuildUniqueDestinationPath(_options.ErrorPath, Path.GetFileName(sourcePath));
                System.IO.File.Move(sourcePath, destination);
            }
            catch (Exception moveEx)
            {
                _logger.LogError(moveEx, "Failed to move scanner file {FilePath} to error path", sourcePath);
            }
        }

        private static string BuildUniqueDestinationPath(string directory, string fileName)
        {
            Directory.CreateDirectory(directory);
            var destination = Path.Combine(directory, fileName);
            if (!System.IO.File.Exists(destination))
            {
                return destination;
            }

            var name = Path.GetFileNameWithoutExtension(fileName);
            var ext = Path.GetExtension(fileName);
            return Path.Combine(directory, $"{name}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}");
        }

        private readonly record struct FileObservation(long SizeBytes, DateTime UnchangedSinceUtc);
    }
}
