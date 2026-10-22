using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Application.Common.Exceptions;

namespace ArchiveSystem.Application.AccessControl
{
    public class AccessAdministrationService : IAccessAdministrationService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IClassificationAclRepository _classificationAclRepository;
        private readonly ITagAclRepository _tagAclRepository;
        private readonly IAccessControlService _accessControlService;

        public AccessAdministrationService(
            IRoleRepository roleRepository,
            IPermissionRepository permissionRepository,
            IUserRoleRepository userRoleRepository,
            IClassificationAclRepository classificationAclRepository,
            ITagAclRepository tagAclRepository,
            IAccessControlService accessControlService)
        {
            _roleRepository = roleRepository;
            _permissionRepository = permissionRepository;
            _userRoleRepository = userRoleRepository;
            _classificationAclRepository = classificationAclRepository;
            _tagAclRepository = tagAclRepository;
            _accessControlService = accessControlService;
        }

        public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
        {
            var roles = await _roleRepository.GetAllAsync(cancellationToken);
            return roles.Select(x => new RoleDto(x.Id, x.Name, x.IsSystem)).ToList();
        }

        public async Task<RoleDto?> GetRoleByIdAsync(long roleId, CancellationToken cancellationToken = default)
        {
            var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
            return role == null ? null : new RoleDto(role.Id, role.Name, role.IsSystem);
        }

        public async Task<RoleDto> CreateRoleAsync(string name, bool isSystem, CancellationToken cancellationToken = default)
        {
            var role = new Role(0, name, isSystem);
            await _roleRepository.AddAsync(role, cancellationToken);
            await _roleRepository.SaveChangesAsync(cancellationToken);
            _accessControlService.InvalidateAllCaches();
            return new RoleDto(role.Id, role.Name, role.IsSystem);
        }

        public async Task<RoleDto?> UpdateRoleAsync(long roleId, string name, CancellationToken cancellationToken = default)
        {
            var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
            if (role == null)
            {
                return null;
            }

            role.Rename(name);
            await _roleRepository.UpdateAsync(role, cancellationToken);
            await _roleRepository.SaveChangesAsync(cancellationToken);
            _accessControlService.InvalidateAllCaches();
            return new RoleDto(role.Id, role.Name, role.IsSystem);
        }

        public async Task<bool> DeleteRoleAsync(long roleId, CancellationToken cancellationToken = default)
        {
            var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
            if (role == null || role.IsSystem)
            {
                return false;
            }

            await _roleRepository.DeleteAsync(role, cancellationToken);
            await _roleRepository.SaveChangesAsync(cancellationToken);
            _accessControlService.InvalidateAllCaches();
            return true;
        }

        public async Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default)
        {
            var permissions = await _permissionRepository.GetAllAsync(cancellationToken);
            return permissions
                .Select(x => new PermissionDto(x.Id, x.Key, x.Description))
                .ToList();
        }

        public async Task<IReadOnlyCollection<long>> GetRolePermissionIdsAsync(long roleId, CancellationToken cancellationToken = default)
        {
            var permissions = await _permissionRepository.GetByRoleIdAsync(roleId, cancellationToken);
            return permissions.Select(x => x.Id).ToArray();
        }

        public async Task SetRolePermissionsAsync(long roleId, IReadOnlyCollection<long> permissionIds, CancellationToken cancellationToken = default)
        {
            await _permissionRepository.SetRolePermissionsAsync(roleId, permissionIds, cancellationToken);
            var affectedUserIds = await _userRoleRepository.GetUserIdsByRoleIdsAsync(new[] { roleId }, cancellationToken);
            foreach (var userId in affectedUserIds)
            {
                _accessControlService.InvalidateUserCache(userId);
            }
        }

        public async Task<IReadOnlyList<RoleDto>> GetUserRolesAsync(long userId, CancellationToken cancellationToken = default)
        {
            var roleIds = await _userRoleRepository.GetRoleIdsByUserIdAsync(userId, cancellationToken);
            if (roleIds.Count == 0)
            {
                return Array.Empty<RoleDto>();
            }

            var roleIdSet = roleIds.ToHashSet();
            var allRoles = await _roleRepository.GetAllAsync(cancellationToken);
            return allRoles
                .Where(role => roleIdSet.Contains(role.Id))
                .OrderBy(role => role.Name)
                .Select(role => new RoleDto(role.Id, role.Name, role.IsSystem))
                .ToArray();
        }

        public async Task SetUserRolesAsync(long userId, IReadOnlyCollection<long> roleIds, CancellationToken cancellationToken = default)
        {
            var normalizedRoleIds = roleIds
                .Where(id => id > 0)
                .Distinct()
                .ToArray();

            var allRoles = await _roleRepository.GetAllAsync(cancellationToken);
            var knownRoleIds = allRoles.Select(role => role.Id).ToHashSet();
            var missingRoleIds = normalizedRoleIds
                .Where(roleId => !knownRoleIds.Contains(roleId))
                .OrderBy(roleId => roleId)
                .ToArray();

            if (missingRoleIds.Length > 0)
            {
                throw new ValidationException(
                    $"The following roles do not exist: {string.Join(", ", missingRoleIds)}.");
            }

            await _userRoleRepository.SetUserRolesAsync(userId, normalizedRoleIds, cancellationToken);
            _accessControlService.InvalidateUserCache(userId);
        }

        public async Task<IReadOnlyList<ClassificationAclEntryDto>> GetClassificationAclAsync(
            long classificationId,
            bool includeDescendants = false,
            CancellationToken cancellationToken = default)
        {
            var entries = includeDescendants
                ? await _classificationAclRepository.GetByClassificationIdAndDescendantsAsync(classificationId, cancellationToken)
                : await _classificationAclRepository.GetByClassificationIdAsync(classificationId, cancellationToken);

            return entries.Select(x => new ClassificationAclEntryDto(
                    x.Id,
                    x.SubjectType,
                    x.SubjectId,
                    x.ClassificationId,
                    x.PermissionMask,
                    x.IsExplicit,
                    x.InheritMode,
                    false))
                .ToList();
        }

        public async Task UpdateClassificationAclAsync(long classificationId, IReadOnlyCollection<ClassificationAclEntryDto> entries, CancellationToken cancellationToken = default)
        {
            var mapped = entries.Select(x =>
                    new ClassificationAclEntry(
                        x.Id,
                        x.SubjectType,
                        x.SubjectId,
                        classificationId,
                        x.PermissionMask,
                        x.IsExplicit,
                        x.InheritMode))
                .ToList();

            await _classificationAclRepository.ReplaceForClassificationAsync(
                classificationId,
                mapped,
                cancellationToken);

            var cascadeSubjectKeys = entries
                .Where(entry => entry.Cascade)
                .Select(entry => (entry.SubjectType, entry.SubjectId))
                .ToHashSet();
            var cascadingEntries = mapped
                .Where(entry => cascadeSubjectKeys.Contains((entry.SubjectType, entry.SubjectId)))
                .ToArray();
            if (cascadingEntries.Length > 0)
            {
                await _classificationAclRepository.OverrideForDescendantsAsync(
                    classificationId,
                    cascadingEntries,
                    cancellationToken);
            }

            _accessControlService.InvalidateAllCaches();
        }

        public async Task<IReadOnlyList<TagAclEntryDto>> GetTagAclAsync(long tagId, CancellationToken cancellationToken = default)
        {
            var entries = await _tagAclRepository.GetByTagIdAsync(tagId, cancellationToken);
            return entries.Select(x => new TagAclEntryDto(
                    x.Id,
                    x.SubjectType,
                    x.SubjectId,
                    x.TagId,
                    x.PermissionMask))
                .ToList();
        }

        public async Task UpdateTagAclAsync(long tagId, IReadOnlyCollection<TagAclEntryDto> entries, CancellationToken cancellationToken = default)
        {
            var mapped = entries.Select(x =>
                    new TagAclEntry(
                        x.Id,
                        x.SubjectType,
                        x.SubjectId,
                        tagId,
                        x.PermissionMask))
                .ToList();

            await _tagAclRepository.ReplaceForTagAsync(tagId, mapped, cancellationToken);
            _accessControlService.InvalidateAllCaches();
        }
    }
}
