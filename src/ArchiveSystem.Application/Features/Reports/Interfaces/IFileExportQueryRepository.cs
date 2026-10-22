using ArchiveSystem.Application.Features.Reports.Models;

namespace ArchiveSystem.Application.Features.Reports.Interfaces
{
    public interface IFileExportQueryRepository
    {
        IAsyncEnumerable<FileExportItemDto> StreamFilesForZipExportAsync(
            FilesZipExportRequest request,
            ActivityReportContext context,
            int batchSize,
            CancellationToken cancellationToken = default);
    }
}
