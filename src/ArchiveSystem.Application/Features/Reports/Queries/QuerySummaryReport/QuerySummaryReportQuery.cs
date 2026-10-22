using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.Reports.Models;
using MediatR;

namespace ArchiveSystem.Application.Features.Reports.Queries.QuerySummaryReport
{
    public sealed class QuerySummaryReportQuery : IRequest<ReportSummaryDto>
    {
        public required ActivityReportFilter Filter { get; init; }
        public required ActivityReportContext Context { get; init; }
    }

    public sealed class QuerySummaryReportQueryHandler : IRequestHandler<QuerySummaryReportQuery, ReportSummaryDto>
    {
        private readonly IMovementLogRepository _movementLogRepository;

        public QuerySummaryReportQueryHandler(IMovementLogRepository movementLogRepository)
        {
            _movementLogRepository = movementLogRepository;
        }

        public Task<ReportSummaryDto> Handle(QuerySummaryReportQuery request, CancellationToken cancellationToken)
        {
            return _movementLogRepository.QuerySummaryAsync(request.Filter, request.Context, cancellationToken);
        }
    }
}
