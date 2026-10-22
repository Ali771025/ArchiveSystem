using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.Reports.Models;
using MediatR;

namespace ArchiveSystem.Application.Features.Reports.Queries.DownloadJobResult
{
    public sealed class DownloadJobResultQuery : IRequest<ExportDownloadResult?>
    {
        public Guid JobId { get; set; }
        public long RequesterUserId { get; set; }
        public bool AllowManageAll { get; set; }
    }

    public sealed class DownloadJobResultQueryHandler : IRequestHandler<DownloadJobResultQuery, ExportDownloadResult?>
    {
        private readonly IExportService _exportService;

        public DownloadJobResultQueryHandler(IExportService exportService)
        {
            _exportService = exportService;
        }

        public Task<ExportDownloadResult?> Handle(DownloadJobResultQuery request, CancellationToken cancellationToken)
        {
            return _exportService.GetJobDownloadAsync(request.JobId, request.RequesterUserId, request.AllowManageAll, cancellationToken);
        }
    }
}
