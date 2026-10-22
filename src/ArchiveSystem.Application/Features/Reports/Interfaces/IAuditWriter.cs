using ArchiveSystem.Application.Features.Reports.Models;

namespace ArchiveSystem.Application.Features.Reports.Interfaces
{
    public interface IAuditWriter
    {
        Task WriteAsync(MovementLogWriteModel model, CancellationToken cancellationToken = default);
    }
}
