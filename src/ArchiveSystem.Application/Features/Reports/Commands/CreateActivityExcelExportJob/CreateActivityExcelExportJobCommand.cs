using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.Reports.Models;
using MediatR;

namespace ArchiveSystem.Application.Features.Reports.Commands.CreateActivityExcelExportJob
{
    public sealed class CreateActivityExcelExportJobCommand : IRequest<ExportJobDto>
    {
        public long UserId { get; set; }
        public ActivityExcelExportRequest Request { get; set; } = new();
    }

    public sealed class CreateActivityExcelExportJobCommandHandler : IRequestHandler<CreateActivityExcelExportJobCommand, ExportJobDto>
    {
        private readonly IExportService _exportService;

        public CreateActivityExcelExportJobCommandHandler(IExportService exportService)
        {
            _exportService = exportService;
        }

        public Task<ExportJobDto> Handle(CreateActivityExcelExportJobCommand request, CancellationToken cancellationToken)
        {
            return _exportService.CreateActivityExcelJobAsync(request.UserId, request.Request, cancellationToken);
        }
    }
}
