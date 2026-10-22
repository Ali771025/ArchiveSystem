using ArchiveSystem.Application.AccessControl;
using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ArchiveSystem.Domain.Enums;

namespace ArchiveSystem.Infrastructure.Services
{
    public class AccessControlService : IAccessControlService
    {
        private readonly IAuthorizationCache _authorizationCache;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IClassificationAclRepository _classificationAclRepository;
        private readonly ITagAclRepository _tagAclRepository;
        private readonly IAccessControlLookupRepository _lookupRepository;
        private readonly ArchiveDbContext _dbContext;

        public AccessControlService(
            IAuthorizationCache authorizationCache,
            IUserRoleRepository userRoleRepository,
            IClassificationAclRepository classificationAclRepository,
            ITagAclRepository tagAclRepository,
            IAccessControlLookupRepository lookupRepository,
            ArchiveDbContext dbContext)
        {
            _authorizationCache = authorizationCache;
            _userRoleRepository = userRoleRepository;
            _classificationAclRepository = classificationAclRepository;
            _tagAclRepository = tagAclRepository;
            _lookupRepository = lookupRepository;
            _dbContext = dbContext;
        }

        public Task<IReadOnlyCollection<long>> EffectiveRolesAsync(long userId, CancellationToken cancellationToken = default)
        {
            return _authorizationCache.GetOrCreateRolesAsync(
                userId,
                () => _userRoleRepository.GetRoleIdsByUserIdAsync(userId, cancellationToken),
                cancellationToken);
        }

        public Task<IReadOnlyDictionary<long, ClassificationPermissionMask>> EffectiveClassificationPermissionsAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            return _authorizationCache.GetOrCreateClassificationPermissionsAsync(
                userId,
                async () =>
                {
                    var roles = await EffectiveRolesAsync(userId, cancellationToken);
                    var nodes = await _lookupRepository.GetClassificationNodesAsync(cancellationToken);
                    var aclEntries = await _classificationAclRepository.GetBySubjectsAsync(userId, roles, cancellationToken);
                    return AccessControlPolicyEngine.ComputeEffectiveClassificationPermissions(nodes, aclEntries);
                },
                cancellationToken);
        }

        public Task<IReadOnlyDictionary<long, TagPermissionMask>> EffectiveTagPermissionsAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            return _authorizationCache.GetOrCreateTagPermissionsAsync(
                userId,
                async () =>
                {
                    var roles = await EffectiveRolesAsync(userId, cancellationToken);
                    var aclEntries = await _tagAclRepository.GetBySubjectsAsync(userId, roles, cancellationToken);
                    return AccessControlPolicyEngine.ComputeEffectiveTagPermissions(aclEntries);
                },
                cancellationToken);
        }

        public async Task<bool> HasGlobalPermissionAsync(long userId, string permissionKey, CancellationToken cancellationToken = default)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(permissionKey))
            {
                return false;
            }

            var roleIds = await EffectiveRolesAsync(userId, cancellationToken);
            if (roleIds.Count == 0)
            {
                return false;
            }

            return await _dbContext.RolePermissions
                .AsNoTracking()
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Join(
                    _dbContext.Permissions.AsNoTracking(),
                    rp => rp.PermissionId,
                    p => p.Id,
                    (_, p) => p.Key)
                .AnyAsync(key => key == permissionKey, cancellationToken);
        }

        public async Task<bool> CanReadClassificationAsync(long userId, long classificationId, CancellationToken cancellationToken = default)
        {
            var map = await EffectiveClassificationPermissionsAsync(userId, cancellationToken);
            return map.TryGetValue(classificationId, out var mask) &&
                   (mask & ClassificationPermissionMask.Read) == ClassificationPermissionMask.Read;
        }

        public async Task<bool> CanWriteClassificationAsync(long userId, long classificationId, CancellationToken cancellationToken = default)
        {
            var map = await EffectiveClassificationPermissionsAsync(userId, cancellationToken);
            return map.TryGetValue(classificationId, out var mask) &&
                   (mask & ClassificationPermissionMask.Write) == ClassificationPermissionMask.Write;
        }

        public async Task<bool> CanMoveClassificationAsync(long userId, long classificationId, CancellationToken cancellationToken = default)
        {
            var map = await EffectiveClassificationPermissionsAsync(userId, cancellationToken);
            return map.TryGetValue(classificationId, out var mask) &&
                   (mask & ClassificationPermissionMask.Move) == ClassificationPermissionMask.Move;
        }

        public async Task<bool> CanManageClassificationAsync(long userId, long classificationId, CancellationToken cancellationToken = default)
        {
            var map = await EffectiveClassificationPermissionsAsync(userId, cancellationToken);
            return map.TryGetValue(classificationId, out var mask) &&
                   (mask & ClassificationPermissionMask.Manage) == ClassificationPermissionMask.Manage;
        }

        public async Task<IReadOnlyCollection<long>> GetAccessibleClassificationIdsAsync(
            long userId,
            ClassificationPermissionMask requiredMask,
            CancellationToken cancellationToken = default)
        {
            var map = await EffectiveClassificationPermissionsAsync(userId, cancellationToken);
            return map
                .Where(x => (x.Value & requiredMask) == requiredMask)
                .Select(x => x.Key)
                .ToArray();
        }

        public async Task<bool> CanViewFileAsync(long userId, long fileId, CancellationToken cancellationToken = default)
        {
            var descriptor = await _lookupRepository.GetFileAccessDescriptorAsync(fileId, cancellationToken);
            if (descriptor == null || !descriptor.ClassificationId.HasValue)
            {
                return false;
            }

            return await CanReadClassificationAsync(
                userId,
                descriptor.ClassificationId.Value,
                cancellationToken);
        }

        public Task<bool> CanUploadFileVersionAsync(long userId, long classificationId, CancellationToken cancellationToken = default)
        {
            return CanWriteClassificationAsync(userId, classificationId, cancellationToken);
        }

        public async Task<bool> CanAssignTagAsync(long userId, long tagId, CancellationToken cancellationToken = default)
        {
            var tagPermissions = await EffectiveTagPermissionsAsync(userId, cancellationToken);
            return tagPermissions.TryGetValue(tagId, out var mask) &&
                   (mask & TagPermissionMask.Assign) == TagPermissionMask.Assign;
        }

        public async Task<IReadOnlyCollection<long>> GetAllowedTagIdsAsync(long userId, TagPermissionMask requiredMask, CancellationToken cancellationToken = default)
        {
            var map = await EffectiveTagPermissionsAsync(userId, cancellationToken);
            return map
                .Where(x => (x.Value & requiredMask) == requiredMask)
                .Select(x => x.Key)
                .ToArray();
        }

        public void InvalidateUserCache(long userId)
        {
            _authorizationCache.InvalidateUser(userId);
        }

        public void InvalidateAllCaches()
        {
            _authorizationCache.InvalidateAll();
        }
    }
}
