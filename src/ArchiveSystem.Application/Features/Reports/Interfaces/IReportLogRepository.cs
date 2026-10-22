using ArchiveSystem.Application.Features.Reports.Models;

namespace ArchiveSystem.Application.Features.Reports.Interfaces;

public interface IReportLogRepository
{
    Task<PagedResultDto<AuditLogResponseDto>> GetAuditTrailAsync(
        AuditFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<AuditFilterOptionsDto> GetAuditFilterOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<SecurityLogResponseDto>> GetSecurityLogsAsync(
        SecurityLogFilterDto filter,
        CancellationToken cancellationToken = default);
}
