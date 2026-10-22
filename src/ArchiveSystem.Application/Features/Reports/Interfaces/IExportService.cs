using ArchiveSystem.Application.Features.Reports.Models;

namespace ArchiveSystem.Application.Features.Reports.Interfaces
{
    public interface IExportService
    {
        Task<ExportJobDto> CreateActivityExcelJobAsync(long userId, ActivityExcelExportRequest request, CancellationToken cancellationToken = default);
        Task<ExportJobDto> CreateFilesZipJobAsync(long userId, FilesZipExportRequest request, CancellationToken cancellationToken = default);
        Task<ExportJobDto?> GetJobStatusAsync(Guid jobId, long requesterUserId, bool allowManageAll, CancellationToken cancellationToken = default);
        Task<ExportDownloadResult?> GetJobDownloadAsync(Guid jobId, long requesterUserId, bool allowManageAll, CancellationToken cancellationToken = default);
    }
}
