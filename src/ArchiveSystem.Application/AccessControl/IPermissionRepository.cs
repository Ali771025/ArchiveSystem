using ArchiveSystem.Domain.Entities;

namespace ArchiveSystem.Application.AccessControl
{
    public interface IPermissionRepository
    {
        Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Permission>> GetByRoleIdAsync(long roleId, CancellationToken cancellationToken = default);
        Task SetRolePermissionsAsync(long roleId, IReadOnlyCollection<long> permissionIds, CancellationToken cancellationToken = default);
    }
}
