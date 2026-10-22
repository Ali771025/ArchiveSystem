namespace ArchiveSystem.Application.AccessControl
{
    public interface IAccessAdministrationService
    {
        Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);
        Task<RoleDto?> GetRoleByIdAsync(long roleId, CancellationToken cancellationToken = default);
        Task<RoleDto> CreateRoleAsync(string name, bool isSystem, CancellationToken cancellationToken = default);
        Task<RoleDto?> UpdateRoleAsync(long roleId, string name, CancellationToken cancellationToken = default);
        Task<bool> DeleteRoleAsync(long roleId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<long>> GetRolePermissionIdsAsync(long roleId, CancellationToken cancellationToken = default);
        Task SetRolePermissionsAsync(long roleId, IReadOnlyCollection<long> permissionIds, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<RoleDto>> GetUserRolesAsync(long userId, CancellationToken cancellationToken = default);
        Task SetUserRolesAsync(long userId, IReadOnlyCollection<long> roleIds, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ClassificationAclEntryDto>> GetClassificationAclAsync(long classificationId, bool includeDescendants = false, CancellationToken cancellationToken = default);
        Task UpdateClassificationAclAsync(long classificationId, IReadOnlyCollection<ClassificationAclEntryDto> entries, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TagAclEntryDto>> GetTagAclAsync(long tagId, CancellationToken cancellationToken = default);
        Task UpdateTagAclAsync(long tagId, IReadOnlyCollection<TagAclEntryDto> entries, CancellationToken cancellationToken = default);
    }
}
