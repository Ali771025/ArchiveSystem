// src/ArchiveSystem.Worker.Indexer/Workers/IndexerWorker.cs
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Infrastructure.Persistence;
using ArchiveSystem.Worker.Indexer.Consumers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ArchiveSystem.Worker.Indexer.Workers
{
    public class IndexerWorker : BackgroundService
    {
        private const string HealthMarkerPath = "/tmp/archive-worker/indexer.started";
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<IndexerWorker> _logger;
        private readonly TimeSpan _backfillInterval;
        private readonly int _backfillBatchSize;
        private readonly HashSet<long> _indexedVersionIds = new();

        public IndexerWorker(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<IndexerWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _backfillInterval = TimeSpan.FromSeconds(Math.Max(
                10,
                configuration.GetValue<int?>("IndexerWorker:BackfillIntervalSeconds") ?? 30));
            _backfillBatchSize = Math.Clamp(
                configuration.GetValue<int?>("IndexerWorker:BackfillBatchSize") ?? 500,
                1,
                5000);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            TouchHealthMarker();
            _logger.LogInformation("Indexer Worker running at: {Time}", DateTimeOffset.Now);

            while (!stoppingToken.IsCancellationRequested)
            {
                TouchHealthMarker();
                await BackfillCompletedOcrVersionsAsync(stoppingToken);
                await Task.Delay(_backfillInterval, stoppingToken);
            }

            _logger.LogInformation("Indexer Worker stopped at: {Time}", DateTimeOffset.Now);
        }

        private async Task BackfillCompletedOcrVersionsAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ArchiveDbContext>();
                var indexer = scope.ServiceProvider.GetRequiredService<OcrCompletedEventConsumer>();

                var candidates = await dbContext.FileVersions
                    .AsNoTracking()
                    .Where(version =>
                        version.DeletedAt == null &&
                        version.OcrStatus == ProcessingStatus.Completed &&
                        version.OcrResult != null &&
                        version.OcrResult != string.Empty)
                    .OrderByDescending(version => version.VersionNumber)
                    .ThenByDescending(version => version.Id)
                    .Select(version => new { version.Id, version.FileId })
                    .Take(_backfillBatchSize * 4)
                    .ToListAsync(cancellationToken);

                var latestVersions = candidates
                    .GroupBy(version => version.FileId)
                    .Select(group => group.First())
                    .Where(version => !_indexedVersionIds.Contains(version.Id))
                    .Take(_backfillBatchSize)
                    .ToList();

                foreach (var version in latestVersions)
                {
                    try
                    {
                        await indexer.IndexFileVersionAsync(version.FileId, version.Id, cancellationToken);
                        _indexedVersionIds.Add(version.Id);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Failed to backfill OCR index for file version {FileVersionId}; it will be retried.",
                            version.Id);
                    }
                }

                if (latestVersions.Count > 0)
                {
                    _logger.LogInformation(
                        "OpenSearch OCR backfill processed {Count} latest file versions.",
                        latestVersions.Count);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OpenSearch OCR backfill polling failed; it will be retried.");
            }
        }

        private static void TouchHealthMarker()
        {
            Directory.CreateDirectory("/tmp/archive-worker");
            File.WriteAllText(HealthMarkerPath, DateTimeOffset.UtcNow.ToString("O"));
        }
    }
}
