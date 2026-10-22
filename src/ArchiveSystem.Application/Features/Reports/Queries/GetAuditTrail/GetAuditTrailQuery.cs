using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.Reports.Models;
using MediatR;

namespace ArchiveSystem.Application.Features.Reports.Queries.GetAuditTrail;

public sealed record GetAuditTrailQuery(AuditFilterDto Filter) : IRequest<PagedResultDto<AuditLogResponseDto>>;

public sealed class GetAuditTrailQueryHandler
    : IRequestHandler<GetAuditTrailQuery, PagedResultDto<AuditLogResponseDto>>
{
    private readonly IReportLogRepository _repository;

    public GetAuditTrailQueryHandler(IReportLogRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResultDto<AuditLogResponseDto>> Handle(
        GetAuditTrailQuery request,
        CancellationToken cancellationToken)
    {
        return _repository.GetAuditTrailAsync(request.Filter, cancellationToken);
    }
}
