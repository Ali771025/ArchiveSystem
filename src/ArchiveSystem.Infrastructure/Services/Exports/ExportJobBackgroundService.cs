using ArchiveSystem.Application.Features.Reports.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ArchiveSystem.Infrastructure.Services.Exports
{
    public class ExportJobBackgroundService : BackgroundService
    {
        private static readonly TimeSpan ActivePollInterval = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan IdlePollInterval = TimeSpan.FromSeconds(15);
        private const int MaxConcurrency = 2;
        private const int MaxBatchSize = 20;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ExportJobBackgroundService> _logger;
        private readonly SemaphoreSlim _concurrencyGate = new(MaxConcurrency, MaxConcurrency);
        private readonly List<Task> _runningTasks = new();

        public ExportJobBackgroundService(IServiceScopeFactory scopeFactory, ILogger<ExportJobBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await MarkStaleRunningJobsAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var nextDelay = IdlePollInterval;
                try
                {
                    var availableSlots = _concurrencyGate.CurrentCount;
                    if (availableSlots <= 0)
                    {
                        _runningTasks.RemoveAll(x => x.IsCompleted);
                        await Task.Delay(ActivePollInterval, stoppingToken);
                        continue;
                    }

                    var pendingJobIds = await GetPendingJobIdsAsync(Math.Min(MaxBatchSize, availableSlots), stoppingToken);
                    nextDelay = pendingJobIds.Count > 0 ? ActivePollInterval : IdlePollInterval;

                    foreach (var jobId in pendingJobIds)
                    {
                        await _concurrencyGate.WaitAsync(stoppingToken);
                        var task = ProcessJobAsync(jobId, stoppingToken).ContinueWith(_ => _concurrencyGate.Release());
                        _runningTasks.Add(task);
                    }

                    _runningTasks.RemoveAll(x => x.IsCompleted);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed while polling export jobs.");
                }

                await Task.Delay(nextDelay, stoppingToken);
            }

            if (_runningTasks.Count > 0)
            {
                await Task.WhenAll(_runningTasks);
            }
        }

        private async Task<IReadOnlyList<Guid>> GetPendingJobIdsAsync(int take, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IExportJobRepository>();
            return await repository.GetPendingJobIdsAsync(take, cancellationToken);
        }

        private async Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IExportJobRepository>();
                var executionService = scope.ServiceProvider.GetRequiredService<IExportJobExecutionService>();

                var claimed = await repository.TryMarkRunningAsync(jobId, cancellationToken);
                if (!claimed)
                {
                    return;
                }

                await executionService.ExecuteAsync(jobId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error while processing export job {JobId}.", jobId);
            }
        }

        private async Task MarkStaleRunningJobsAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IExportJobRepository>();
                await repository.MarkStaleRunningJobsFailedAsync("Worker restarted while job was running.", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark stale running export jobs.");
            }
        }
    }
}
