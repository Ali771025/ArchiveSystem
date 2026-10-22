using ArchiveSystem.Domain.Entities;

namespace ArchiveSystem.Application.Features.Reports.Interfaces
{
    public interface IExportJobRepository
    {
        Task AddAsync(ExportJob job, CancellationToken cancellationToken = default);
        Task<ExportJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default);
        Task<ExportJob?> GetByIdForUpdateAsync(Guid jobId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Guid>> GetPendingJobIdsAsync(int take, CancellationToken cancellationToken = default);
        Task<bool> TryMarkRunningAsync(Guid jobId, CancellationToken cancellationToken = default);
        Task MarkStaleRunningJobsFailedAsync(string reason, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
