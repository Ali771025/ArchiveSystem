using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.Reports.Models;
using MediatR;

namespace ArchiveSystem.Application.Features.Reports.Commands.CreateFilesZipExportJob
{
    public sealed class CreateFilesZipExportJobCommand : IRequest<ExportJobDto>
    {
        public long UserId { get; set; }
        public FilesZipExportRequest Request { get; set; } = new();
    }

    public sealed class CreateFilesZipExportJobCommandHandler : IRequestHandler<CreateFilesZipExportJobCommand, ExportJobDto>
    {
        private readonly IExportService _exportService;

        public CreateFilesZipExportJobCommandHandler(IExportService exportService)
        {
            _exportService = exportService;
        }

        public Task<ExportJobDto> Handle(CreateFilesZipExportJobCommand request, CancellationToken cancellationToken)
        {
            return _exportService.CreateFilesZipJobAsync(request.UserId, request.Request, cancellationToken);
        }
    }
}
