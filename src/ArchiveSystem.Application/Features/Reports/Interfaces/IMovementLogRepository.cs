using ArchiveSystem.Application.Common.Models;
using ArchiveSystem.Application.Features.Reports.Models;
using ArchiveSystem.Domain.Entities;

namespace ArchiveSystem.Application.Features.Reports.Interfaces
{
    public interface IMovementLogRepository
    {
        Task AddAsync(MovementLog movementLog, CancellationToken cancellationToken = default);

        Task<PaginatedResult<ActivityReportItemDto>> QueryActivityAsync(
            ActivityReportFilter filter,
            ActivityReportContext context,
            CancellationToken cancellationToken = default);

        Task<ReportSummaryDto> QuerySummaryAsync(
            ActivityReportFilter filter,
            ActivityReportContext context,
            CancellationToken cancellationToken = default);

        IAsyncEnumerable<ActivityReportItemDto> StreamActivityAsync(
            ActivityReportFilter filter,
            ActivityReportContext context,
            int batchSize,
            CancellationToken cancellationToken = default);
    }
}
