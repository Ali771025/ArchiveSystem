using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.Reports.Models;
using MediatR;

namespace ArchiveSystem.Application.Features.Reports.Queries.GetJobStatus
{
    public sealed class GetJobStatusQuery : IRequest<ExportJobDto?>
    {
        public Guid JobId { get; set; }
        public long RequesterUserId { get; set; }
        public bool AllowManageAll { get; set; }
    }

    public sealed class GetJobStatusQueryHandler : IRequestHandler<GetJobStatusQuery, ExportJobDto?>
    {
        private readonly IExportService _exportService;

        public GetJobStatusQueryHandler(IExportService exportService)
        {
            _exportService = exportService;
        }

        public Task<ExportJobDto?> Handle(GetJobStatusQuery request, CancellationToken cancellationToken)
        {
            return _exportService.GetJobStatusAsync(request.JobId, request.RequesterUserId, request.AllowManageAll, cancellationToken);
        }
    }
}
