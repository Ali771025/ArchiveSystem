using ArchiveSystem.Domain.Entities;
using DomainFile = ArchiveSystem.Domain.Entities.ArchiveFile;

namespace ArchiveSystem.Application.Interfaces.Persistence
{
    public interface IFileRepository
    {
        Task<DomainFile?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<DomainFile?> FindActiveByCaseAndNameAsync(long caseId, string name, CancellationToken cancellationToken = default);
        Task AddAsync(DomainFile file, CancellationToken cancellationToken = default);
        Task UpdateAsync(DomainFile file, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
