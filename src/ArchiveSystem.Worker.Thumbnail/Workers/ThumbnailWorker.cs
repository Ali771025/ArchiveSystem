using ArchiveSystem.Application.Interfaces.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ArchiveSystem.Worker.Thumbnail.Options;
using ArchiveSystem.Worker.Thumbnail.Services;

namespace ArchiveSystem.Worker.Thumbnail.Workers
{
    public sealed class ThumbnailWorker : BackgroundService
    {
        private const string HealthMarkerPath = "/tmp/archive-worker/thumbnail.started";
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptionsMonitor<ThumbnailOptions> _optionsMonitor;
        private readonly ILogger<ThumbnailWorker> _logger;

        public ThumbnailWorker(
            IServiceScopeFactory scopeFactory,
            IOptionsMonitor<ThumbnailOptions> optionsMonitor,
            ILogger<ThumbnailWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _optionsMonitor = optionsMonitor;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await TouchHealthMarkerAsync(stoppingToken);
            _logger.LogInformation("Thumbnail worker started");

            while (!stoppingToken.IsCancellationRequested)
            {
                await TouchHealthMarkerAsync(stoppingToken);

                try
                {
                    var options = _optionsMonitor.CurrentValue;
                    var pendingVersionIds = await GetPendingVersionIdsAsync(options.BatchSize, stoppingToken);
                    if (pendingVersionIds.Count > 0)
                    {
                        await ProcessBatchAsync(pendingVersionIds, options.MaxConcurrency, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Thumbnail polling loop failed");
                }

                var pollInterval = Math.Max(1, _optionsMonitor.CurrentValue.PollIntervalSeconds);
                await Task.Delay(TimeSpan.FromSeconds(pollInterval), stoppingToken);
            }
        }

        private async Task<IReadOnlyList<long>> GetPendingVersionIdsAsync(int batchSize, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IFileVersionRepository>();
            return await repository.GetPendingThumbnailVersionIdsAsync(batchSize, cancellationToken);
        }

        private async Task ProcessBatchAsync(
            IReadOnlyList<long> pendingVersionIds,
            int maxConcurrency,
            CancellationToken cancellationToken)
        {
            var concurrency = Math.Max(1, maxConcurrency);
            var completed = 0;
            var failed = 0;
            var skipped = 0;

            _logger.LogInformation(
                "Starting thumbnail batch. Size: {BatchSize}, MaxConcurrency: {MaxConcurrency}",
                pendingVersionIds.Count,
                concurrency);

            using var throttler = new SemaphoreSlim(concurrency, concurrency);
            var tasks = pendingVersionIds.Select(async versionId =>
            {
                await throttler.WaitAsync(cancellationToken);
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var processingService = scope.ServiceProvider.GetRequiredService<ThumbnailProcessingService>();
                    var result = await processingService.ProcessFileVersionAsync(versionId, cancellationToken);
                    switch (result)
                    {
                        case ThumbnailProcessResult.Completed:
                            Interlocked.Increment(ref completed);
                            break;
                        case ThumbnailProcessResult.Failed:
                            Interlocked.Increment(ref failed);
                            break;
                        default:
                            Interlocked.Increment(ref skipped);
                            break;
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failed);
                    _logger.LogError(ex, "Unhandled thumbnail processing error for file version {FileVersionId}", versionId);
                }
                finally
                {
                    throttler.Release();
                }
            }).ToArray();

            await Task.WhenAll(tasks);

            _logger.LogInformation(
                "Completed thumbnail batch. Requested: {Requested}, Completed: {Completed}, Failed: {Failed}, Skipped: {Skipped}",
                pendingVersionIds.Count,
                completed,
                failed,
                skipped);
        }

        private static async Task TouchHealthMarkerAsync(CancellationToken cancellationToken)
        {
            Directory.CreateDirectory("/tmp/archive-worker");
            await File.WriteAllTextAsync(HealthMarkerPath, DateTimeOffset.UtcNow.ToString("O"), cancellationToken);
        }
    }
}
