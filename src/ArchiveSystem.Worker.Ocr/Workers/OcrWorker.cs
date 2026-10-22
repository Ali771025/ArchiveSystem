using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Worker.Ocr.Options;
using ArchiveSystem.Worker.Ocr.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchiveSystem.Worker.Ocr.Workers
{
    public class OcrWorker : BackgroundService
    {
        private const string HealthMarkerPath = "/tmp/archive-worker/ocr.started";
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly OcrWorkerOptions _options;
        private readonly RapidOcrEngineOptions _rapidOcrOptions;
        private readonly ILogger<OcrWorker> _logger;
        private DateTime _lastStaleResetUtc = DateTime.MinValue;

        public OcrWorker(
            IServiceScopeFactory scopeFactory,
            IOptions<OcrWorkerOptions> options,
            IOptions<RapidOcrEngineOptions> rapidOcrOptions,
            ILogger<OcrWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _rapidOcrOptions = rapidOcrOptions.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!RapidOcrOfflineRuntime.TryValidate(
                    _rapidOcrOptions,
                    _logger,
                    out var initializationFailure))
            {
                _logger.LogCritical(
                    "OCR worker is disabled: {Reason} Pending OCR jobs are preserved.",
                    initializationFailure);
                return;
            }

            TouchHealthMarker();
            _logger.LogInformation("OCR worker started");

            while (!stoppingToken.IsCancellationRequested)
            {
                TouchHealthMarker();
                try
                {
                    await ResetStaleProcessingIfDueAsync(stoppingToken);

                    IReadOnlyList<long> pendingVersionIds;
                    await using (var queryScope = _scopeFactory.CreateAsyncScope())
                    {
                        var repository = queryScope.ServiceProvider.GetRequiredService<IFileVersionRepository>();
                        pendingVersionIds = await repository.GetPendingOcrVersionIdsAsync(
                            _options.BatchSize,
                            stoppingToken);
                    }

                    var parallelOptions = new ParallelOptions
                    {
                        CancellationToken = stoppingToken,
                        MaxDegreeOfParallelism = NormalizeMaxConcurrentJobs(_options.MaxConcurrentJobs)
                    };

                    await Parallel.ForEachAsync(pendingVersionIds, parallelOptions, async (versionId, token) =>
                    {
                        await using var processingScope = _scopeFactory.CreateAsyncScope();
                        var processingService = processingScope.ServiceProvider.GetRequiredService<OcrProcessingService>();
                        await processingService.ProcessFileVersionAsync(versionId, token);
                    });
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "OCR polling loop failed");
                }

                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.PollIntervalSeconds)), stoppingToken);
            }
        }

        private async Task ResetStaleProcessingIfDueAsync(CancellationToken cancellationToken)
        {
            if (_options.StaleProcessingTimeoutMinutes <= 0)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var resetInterval = TimeSpan.FromMinutes(Math.Max(1, _options.StaleProcessingResetIntervalMinutes));
            if (now - _lastStaleResetUtc < resetInterval)
            {
                return;
            }

            _lastStaleResetUtc = now;
            var processingTimeout = TimeSpan.FromMinutes(_options.StaleProcessingTimeoutMinutes);
            await using var scope = _scopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IFileVersionRepository>();
            var resetCount = await repository.ResetStaleOcrProcessingAsync(
                processingTimeout,
                $"OCR processing reset after exceeding {processingTimeout.TotalMinutes:0} minute(s).",
                cancellationToken);

            if (resetCount > 0)
            {
                _logger.LogWarning(
                    "Reset {ResetCount} stale OCR processing job(s) older than {TimeoutMinutes} minute(s)",
                    resetCount,
                    processingTimeout.TotalMinutes);
            }
        }

        private static int NormalizeMaxConcurrentJobs(int value)
        {
            if (value <= 0)
            {
                return 1;
            }

            return Math.Min(value, 16);
        }

        private static void TouchHealthMarker()
        {
            Directory.CreateDirectory("/tmp/archive-worker");
            File.WriteAllText(HealthMarkerPath, DateTimeOffset.UtcNow.ToString("O"));
        }
    }
}
