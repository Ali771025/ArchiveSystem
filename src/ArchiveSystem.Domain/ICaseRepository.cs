// src/ArchiveSystem.Domain/ICaseRepository.cs
using ArchiveSystem.Domain.Entities;

namespace ArchiveSystem.Domain.Repositories
{
    public interface ICaseRepository
    {
        Task<Case?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Case>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Case?> GetByCaseNumberAsync(string caseNumber, CancellationToken cancellationToken = default);
        Task<Case?> GetByExternalCaseNumberAsync(string externalCaseNumber, CancellationToken cancellationToken = default);
        Task<Case?> GetByCaseNumberFlexibleAsync(string requestKey, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Case>> GetByClassificationIdAsync(long classificationId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Case>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<long> CountAsync(CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default);
        Task AddAsync(Case caseEntity, CancellationToken cancellationToken = default);
        Task<bool> MoveToClassificationAsync(long caseId, long classificationId, CancellationToken cancellationToken = default);
        Task UpdateAsync(Case caseEntity, CancellationToken cancellationToken = default);
        Task DeleteAsync(Case caseEntity, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
