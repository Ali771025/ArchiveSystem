// src/ArchiveSystem.Domain/IClassificationRepository.cs
using ArchiveSystem.Domain.Entities;

namespace ArchiveSystem.Domain.Repositories
{
    public interface IClassificationRepository
    {
        Task<Classification?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<Classification?> GetBySystemKeyAsync(string systemKey, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Classification>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Classification>> GetRootClassificationsAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Classification>> GetByParentIdAsync(long? parentId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Classification>> GetByPathPrefixAsync(string pathPrefix, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Classification>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<long> CountAsync(CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default);
        Task<bool> HasChildrenAsync(long id, CancellationToken cancellationToken = default);
        Task<bool> ExistsActiveSiblingWithTitleAsync(long? parentId, string title, long? excludeClassificationId = null, CancellationToken cancellationToken = default);
        Task<bool> HasActiveCasesAsync(long id, CancellationToken cancellationToken = default);
        Task<bool> HasActiveDossiersAsync(long id, CancellationToken cancellationToken = default);
        Task<IReadOnlyDictionary<long, int>> GetDossierCountsByClassificationAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Classification>> SearchByTitleAsync(string query, int take = 50, CancellationToken cancellationToken = default);
        Task AddAsync(Classification classification, CancellationToken cancellationToken = default);
        Task UpdateAsync(Classification classification, CancellationToken cancellationToken = default);
        Task DeleteAsync(Classification classification, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
