using ArchiveSystem.Application.Features.Reports.Models;

namespace ArchiveSystem.Infrastructure.Services.Exports
{
    public interface IFilesZipExporter
    {
        Task<int> GenerateAsync(
            string outputPath,
            FilesZipExportRequest request,
            ActivityReportContext context,
            CancellationToken cancellationToken = default);
    }
}
