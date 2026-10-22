using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Infrastructure.Repositories
{
    public class EfExportJobRepository : IExportJobRepository
    {
        private readonly ArchiveDbContext _dbContext;

        public EfExportJobRepository(ArchiveDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(ExportJob job, CancellationToken cancellationToken = default)
        {
            await _dbContext.ExportJobs.AddAsync(job, cancellationToken);
        }

        public async Task<ExportJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.ExportJobs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == jobId, cancellationToken);
        }

        public async Task<ExportJob?> GetByIdForUpdateAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.ExportJobs
                .FirstOrDefaultAsync(x => x.Id == jobId, cancellationToken);
        }

        public async Task<IReadOnlyList<Guid>> GetPendingJobIdsAsync(int take, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var normalizedTake = take <= 0 ? 10 : Math.Min(take, 200);
            return await _dbContext.ExportJobs
                .AsNoTracking()
                .Where(x => x.Status == ExportJobStatus.Pending && x.ExpiresAt > now)
                .OrderBy(x => x.CreatedAt)
                .Select(x => x.Id)
                .Take(normalizedTake)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> TryMarkRunningAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var affected = await _dbContext.ExportJobs
                .Where(x => x.Id == jobId && x.Status == ExportJobStatus.Pending && x.ExpiresAt > now)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, ExportJobStatus.Running)
                    .SetProperty(x => x.StartedAt, now)
                    .SetProperty(x => x.ProgressPercent, 0)
                    .SetProperty(x => x.ErrorMessage, (string?)null)
                    .SetProperty(x => x.ResultObjectKey, (string?)null)
                    .SetProperty(x => x.ModifiedAt, now), cancellationToken);

            return affected > 0;
        }

        public async Task MarkStaleRunningJobsFailedAsync(string reason, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            await _dbContext.ExportJobs
                .Where(x => x.Status == ExportJobStatus.Running)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, ExportJobStatus.Failed)
                    .SetProperty(x => x.ErrorMessage, reason)
                    .SetProperty(x => x.CompletedAt, now)
                    .SetProperty(x => x.ModifiedAt, now), cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
