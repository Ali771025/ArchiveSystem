// src/ArchiveSystem.Domain/Repositories/IAuditLogRepository.cs
using ArchiveSystem.Domain.Entities;

namespace ArchiveSystem.Domain.Repositories
{
    public interface IAuditLogRepository
    {
        Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AuditLog>> GetByUserIdAsync(long userId, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AuditLog>> GetByFileIdAsync(long fileId, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AuditLog>> GetByActionAsync(string action, int page, int pageSize, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}