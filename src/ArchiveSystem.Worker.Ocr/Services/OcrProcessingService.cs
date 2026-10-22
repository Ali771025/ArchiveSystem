using System.Diagnostics;
using System.Text.Json;
using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Application.Interfaces.Storage;
using ArchiveSystem.Application.Options;
using ArchiveSystem.Domain.Events;
using ArchiveSystem.Domain.Services;
using ArchiveSystem.Worker.Ocr.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchiveSystem.Worker.Ocr.Services
{
    public class OcrProcessingService
    {
        private readonly IFileVersionRepository _fileVersionRepository;
        private readonly IObjectStorage _objectStorage;
        private readonly ObjectStorageOptions _objectStorageOptions;
        private readonly OcrWorkerOptions _workerOptions;
        private readonly IOcrService _ocrService;
        private readonly IMessageBus _messageBus;
        private readonly ILogger<OcrProcessingService> _logger;

        public OcrProcessingService(
            IFileVersionRepository fileVersionRepository,
            IObjectStorage objectStorage,
            IOptions<ObjectStorageOptions> objectStorageOptions,
            IOptions<OcrWorkerOptions> workerOptions,
            IOcrService ocrService,
            IMessageBus messageBus,
            ILogger<OcrProcessingService> logger)
        {
            _fileVersionRepository = fileVersionRepository;
            _objectStorage = objectStorage;
            _objectStorageOptions = objectStorageOptions.Value;
            _workerOptions = workerOptions.Value;
            _ocrService = ocrService;
            _messageBus = messageBus;
            _logger = logger;
        }

        public async Task ProcessFileVersionAsync(
            long fileVersionId,
            CancellationToken cancellationToken,
            bool rethrowOnFailure = false)
        {
            using var timeoutCts = CreateTimeoutTokenSource();
            using var linkedCts = timeoutCts is null
                ? null
                : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            var processingToken = linkedCts?.Token ?? cancellationToken;

            if (!await _fileVersionRepository.TryStartOcrProcessingAsync(fileVersionId, processingToken))
            {
                return;
            }

            try
            {
                var version = await _fileVersionRepository.GetByIdAsync(fileVersionId, processingToken);
                if (version == null || version.IsDeleted)
                {
                    await _fileVersionRepository.TryFailOcrAsync(fileVersionId, "File version not found or deleted.", CancellationToken.None);
                    return;
                }

                if (!await _objectStorage.ExistsAsync(_objectStorageOptions.BucketName, version.StoragePath, processingToken))
                {
                    await _fileVersionRepository.TryFailOcrAsync(fileVersionId, "Source object not found in object storage.", CancellationToken.None);
                    return;
                }

                await using var source = await _objectStorage.GetObjectStreamAsync(
                    _objectStorageOptions.BucketName,
                    version.StoragePath,
                    processingToken);

                var stopwatch = Stopwatch.StartNew();
                var result = await _ocrService.ExtractAsync(source, version.FileName, version.ContentType, processingToken);
                stopwatch.Stop();

                if (result.ProcessingDurationMs <= 0)
                {
                    result = result with { ProcessingDurationMs = stopwatch.ElapsedMilliseconds };
                }

                var hasText = result.HasText;

                var payload = JsonSerializer.Serialize(new
                {
                    engine = result.Engine,
                    language = result.Language,
                    detectedLanguages = result.DetectedLanguages,
                    confidence = result.Confidence,
                    wordCount = result.WordCount,
                    processingDurationMs = result.ProcessingDurationMs,
                    pageCount = result.PageCount,
                    hasText = result.HasText,
                    layoutProfile = result.LayoutProfile,
                    template = result.Template,
                    documentType = result.DocumentType,
                    documentConfidence = result.DocumentConfidence,
                    title = result.Title,
                    referenceCode = result.ReferenceCode,
                    warnings = result.Warnings,
                    fields = result.Fields,
                    text = result.Text,
                    pages = result.Pages.Select(page => new
                    {
                        pageNumber = page.PageNumber,
                        confidence = page.Confidence,
                        width = page.Width,
                        height = page.Height,
                        text = page.Text,
                        lines = page.Lines,
                        lineElements = page.LineElements
                    })
                }, OcrJsonSerializer.PayloadOptions);

                if (!await _fileVersionRepository.TryCompleteOcrAsync(
                        fileVersionId,
                        payload,
                        result.Text,
                        result.Language,
                        result.WordCount,
                        processingToken))
                {
                    return;
                }

                await _messageBus.PublishAsync(
                    new OcrCompletedEvent(version.FileId, version.Id, result.Language, result.WordCount, hasText),
                    processingToken);
            }
            catch (OperationCanceledException) when (timeoutCts?.IsCancellationRequested == true && !cancellationToken.IsCancellationRequested)
            {
                var timeout = TimeSpan.FromMinutes(Math.Max(1, _workerOptions.JobTimeoutMinutes));
                var message = $"OCR processing timed out after {timeout.TotalMinutes:0} minute(s).";
                _logger.LogWarning(
                    "OCR processing timed out for file version {FileVersionId} after {TimeoutMinutes} minute(s)",
                    fileVersionId,
                    timeout.TotalMinutes);
                await _fileVersionRepository.TryFailOcrAsync(fileVersionId, message, CancellationToken.None);
                if (rethrowOnFailure)
                {
                    throw new TimeoutException(message);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("OCR processing cancelled for file version {FileVersionId}", fileVersionId);
                throw;
            }
            catch (Exception ex)
            {
                var safeError = SanitizeFailureMessage(ex);
                _logger.LogError(ex, "OCR processing failed for file version {FileVersionId}", fileVersionId);

                await _fileVersionRepository.TryFailOcrAsync(fileVersionId, safeError, CancellationToken.None);
                if (rethrowOnFailure)
                {
                    throw;
                }
            }
        }

        private CancellationTokenSource? CreateTimeoutTokenSource()
        {
            if (_workerOptions.JobTimeoutMinutes <= 0)
            {
                return null;
            }

            return new CancellationTokenSource(TimeSpan.FromMinutes(_workerOptions.JobTimeoutMinutes));
        }

        private static string SanitizeFailureMessage(Exception exception)
        {
            return string.IsNullOrWhiteSpace(exception.Message)
                ? "Unknown OCR processing error."
                : exception.Message;
        }
    }
}
