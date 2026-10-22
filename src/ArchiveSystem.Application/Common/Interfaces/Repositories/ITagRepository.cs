using ArchiveSystem.Domain.Entities;

namespace ArchiveSystem.Application.Common.Interfaces.Repositories
{
    public interface ITagRepository
    {
        Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Tag?> GetByIdAsync(long tagId, CancellationToken cancellationToken = default);
        Task AddAsync(Tag tag, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
