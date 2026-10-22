using ArchiveSystem.Domain.Entities;

namespace ArchiveSystem.Application.AccessControl
{
    public interface IRoleRepository
    {
        Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Role?> GetByIdAsync(long roleId, CancellationToken cancellationToken = default);
        Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
        Task AddAsync(Role role, CancellationToken cancellationToken = default);
        Task UpdateAsync(Role role, CancellationToken cancellationToken = default);
        Task DeleteAsync(Role role, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
