using ArchiveSystem.Application.Features.Reports.Models;

namespace ArchiveSystem.Infrastructure.Services.Exports
{
    public interface IActivityExcelExporter
    {
        Task GenerateAsync(
            string outputPath,
            ActivityReportFilter filter,
            ActivityReportContext context,
            CancellationToken cancellationToken = default);
    }
}
