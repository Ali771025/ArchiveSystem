using ArchiveSystem.Application.Features.AccessControl.DTOs;

namespace ArchiveSystem.Application.Features.AccessControl.Interfaces
{
    public interface IIdentityRoleService
    {
        Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);

        Task<RoleDto?> GetRoleByIdAsync(long roleId, CancellationToken cancellationToken = default);

        Task<RoleDto> CreateRoleAsync(string name, bool isSystem, CancellationToken cancellationToken = default);

        Task<RoleDto> UpdateRoleAsync(long roleId, string name, CancellationToken cancellationToken = default);

        Task DeleteRoleAsync(long roleId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<RoleDto>> GetUserRolesAsync(long userId, CancellationToken cancellationToken = default);

        Task SetUserRolesAsync(long userId, IReadOnlyCollection<long> roleIds, CancellationToken cancellationToken = default);
    }
}
