using ArchiveSystem.Domain.Entities;

namespace ArchiveSystem.Application.Interfaces.Persistence
{
    public interface IUploadSessionRepository
    {
        Task<UploadSession?> GetByIdAsync(Guid id, bool includeParts = false, CancellationToken cancellationToken = default);
        Task AddAsync(UploadSession session, CancellationToken cancellationToken = default);
        Task UpdateAsync(UploadSession session, CancellationToken cancellationToken = default);
        Task UpsertPartAsync(UploadPart part, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<UploadPart>> ListPartsAsync(Guid sessionId, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
