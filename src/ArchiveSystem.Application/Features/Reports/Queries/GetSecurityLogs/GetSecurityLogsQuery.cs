using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.Reports.Models;
using MediatR;

namespace ArchiveSystem.Application.Features.Reports.Queries.GetSecurityLogs;

public sealed record GetSecurityLogsQuery(SecurityLogFilterDto Filter) : IRequest<PagedResultDto<SecurityLogResponseDto>>;

public sealed class GetSecurityLogsQueryHandler
    : IRequestHandler<GetSecurityLogsQuery, PagedResultDto<SecurityLogResponseDto>>
{
    private readonly IReportLogRepository _repository;

    public GetSecurityLogsQueryHandler(IReportLogRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResultDto<SecurityLogResponseDto>> Handle(
        GetSecurityLogsQuery request,
        CancellationToken cancellationToken)
    {
        return _repository.GetSecurityLogsAsync(request.Filter, cancellationToken);
    }
}
