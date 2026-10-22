using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Application.Interfaces.Storage;
using ArchiveSystem.Application.Options;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Events;
using ArchiveSystem.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using ArchiveSystem.Worker.Thumbnail.Options;

namespace ArchiveSystem.Worker.Thumbnail.Services
{
    public enum ThumbnailProcessResult
    {
        Completed,
        Failed,
        Skipped
    }

    public sealed class ThumbnailProcessingService
    {
        private readonly IFileVersionRepository _fileVersionRepository;
        private readonly IObjectStorage _objectStorage;
        private readonly ObjectStorageOptions _storageOptions;
        private readonly IThumbnailGenerator _thumbnailGenerator;
        private readonly ThumbnailOptions _options;
        private readonly IMessageBus _messageBus;
        private readonly ILogger<ThumbnailProcessingService> _logger;

        public ThumbnailProcessingService(
            IFileVersionRepository fileVersionRepository,
            IObjectStorage objectStorage,
            IOptions<ObjectStorageOptions> storageOptions,
            IThumbnailGenerator thumbnailGenerator,
            IOptions<ThumbnailOptions> options,
            IMessageBus messageBus,
            ILogger<ThumbnailProcessingService> logger)
        {
            _fileVersionRepository = fileVersionRepository;
            _objectStorage = objectStorage;
            _storageOptions = storageOptions.Value;
            _thumbnailGenerator = thumbnailGenerator;
            _options = options.Value;
            _messageBus = messageBus;
            _logger = logger;
        }

        public async Task<ThumbnailProcessResult> ProcessFileVersionAsync(long fileVersionId, CancellationToken cancellationToken)
        {
            if (!await _fileVersionRepository.TryStartThumbnailProcessingAsync(fileVersionId, cancellationToken))
            {
                return ThumbnailProcessResult.Skipped;
            }

            var version = await _fileVersionRepository.GetByIdAsync(fileVersionId, cancellationToken);
            if (version == null || version.IsDeleted)
            {
                await _fileVersionRepository.TryFailThumbnailAsync(fileVersionId, "File version not found or deleted.", cancellationToken);
                return ThumbnailProcessResult.Failed;
            }

            var maxAttempts = Math.Max(1, _options.MaxRetryAttempts);
            var baseDelayMs = Math.Max(50, _options.RetryDelayMilliseconds);

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    return await ProcessFileVersionInternalAsync(version, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex) when (ShouldRetry(ex, attempt, maxAttempts))
                {
                    var delay = TimeSpan.FromMilliseconds(baseDelayMs * attempt);
                    _logger.LogWarning(
                        ex,
                        "Retrying thumbnail generation for file version {FileVersionId}. Attempt {Attempt}/{MaxAttempts} in {DelayMs} ms.",
                        fileVersionId,
                        attempt,
                        maxAttempts,
                        (int)delay.TotalMilliseconds);

                    await Task.Delay(delay, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Thumbnail processing failed for file version {FileVersionId} after {Attempt} attempt(s).",
                        fileVersionId,
                        attempt);

                    await _fileVersionRepository.TryFailThumbnailAsync(fileVersionId, ex.Message, cancellationToken);
                    return ThumbnailProcessResult.Failed;
                }
            }

            await _fileVersionRepository.TryFailThumbnailAsync(fileVersionId, "Unknown thumbnail processing failure.", cancellationToken);
            return ThumbnailProcessResult.Failed;
        }

        private async Task<ThumbnailProcessResult> ProcessFileVersionInternalAsync(
            FileVersion version,
            CancellationToken cancellationToken)
        {
            if (!await _objectStorage.ExistsAsync(_storageOptions.BucketName, version.StoragePath, cancellationToken))
            {
                throw new FileNotFoundException(
                    $"Source object '{version.StoragePath}' was not found in '{_storageOptions.BucketName}'.");
            }

            await using var source = await _objectStorage.GetObjectStreamAsync(
                _storageOptions.BucketName,
                version.StoragePath,
                cancellationToken);

            var thumbnailResult = await _thumbnailGenerator.GenerateThumbnailAsync(source, cancellationToken);
            if (!await _fileVersionRepository.TryCompleteThumbnailAsync(
                    version.Id,
                    thumbnailResult.Bytes,
                    thumbnailResult.ContentType,
                    thumbnailResult.Width,
                    thumbnailResult.Height,
                    cancellationToken))
            {
                return ThumbnailProcessResult.Skipped;
            }

            await _messageBus.PublishAsync(
                new ThumbnailGeneratedEvent(
                    version.FileId,
                    version.Id,
                    thumbnailResult.Bytes.Length,
                    thumbnailResult.ContentType,
                    thumbnailResult.Width,
                    thumbnailResult.Height),
                cancellationToken);

            return ThumbnailProcessResult.Completed;
        }

        private static bool ShouldRetry(Exception exception, int attempt, int maxAttempts)
        {
            if (attempt >= maxAttempts)
            {
                return false;
            }

            return exception is not NotSupportedException
                and not UnknownImageFormatException
                and not ArgumentException
                and not FileNotFoundException;
        }
    }
}
