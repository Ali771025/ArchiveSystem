using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.Reports.Models;
using MediatR;

namespace ArchiveSystem.Application.Features.Reports.Queries.GetAuditFilterOptions;

public sealed record GetAuditFilterOptionsQuery : IRequest<AuditFilterOptionsDto>;

public sealed class GetAuditFilterOptionsQueryHandler
    : IRequestHandler<GetAuditFilterOptionsQuery, AuditFilterOptionsDto>
{
    private readonly IReportLogRepository _repository;

    public GetAuditFilterOptionsQueryHandler(IReportLogRepository repository)
    {
        _repository = repository;
    }

    public Task<AuditFilterOptionsDto> Handle(
        GetAuditFilterOptionsQuery request,
        CancellationToken cancellationToken) =>
        _repository.GetAuditFilterOptionsAsync(cancellationToken);
}
