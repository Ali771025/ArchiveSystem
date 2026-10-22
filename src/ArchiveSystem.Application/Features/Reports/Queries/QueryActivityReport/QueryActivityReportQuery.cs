using ArchiveSystem.Application.Common.Models;
using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.Reports.Models;
using MediatR;

namespace ArchiveSystem.Application.Features.Reports.Queries.QueryActivityReport
{
    public sealed class QueryActivityReportQuery : IRequest<PaginatedResult<ActivityReportItemDto>>
    {
        public required ActivityReportFilter Filter { get; init; }
        public required ActivityReportContext Context { get; init; }
    }

    public sealed class QueryActivityReportQueryHandler : IRequestHandler<QueryActivityReportQuery, PaginatedResult<ActivityReportItemDto>>
    {
        private readonly IMovementLogRepository _movementLogRepository;

        public QueryActivityReportQueryHandler(IMovementLogRepository movementLogRepository)
        {
            _movementLogRepository = movementLogRepository;
        }

        public Task<PaginatedResult<ActivityReportItemDto>> Handle(QueryActivityReportQuery request, CancellationToken cancellationToken)
        {
            return _movementLogRepository.QueryActivityAsync(request.Filter, request.Context, cancellationToken);
        }
    }
}
