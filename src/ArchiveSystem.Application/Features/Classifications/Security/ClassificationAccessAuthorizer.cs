using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Common.Interfaces.Security;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;

namespace ArchiveSystem.Application.Features.Classifications.Security
{
    public sealed class ClassificationAccessAuthorizer : IClassificationAccessAuthorizer
    {
        private readonly ICurrentUserContext _currentUser;
        private readonly ArchiveSystem.Application.Common.Interfaces.Security.IAccessControlService _accessControlService;
        private readonly IIdentityManagementService _identityManagementService;

        public ClassificationAccessAuthorizer(
            ICurrentUserContext currentUser,
            ArchiveSystem.Application.Common.Interfaces.Security.IAccessControlService accessControlService,
            IIdentityManagementService identityManagementService)
        {
            _currentUser = currentUser;
            _accessControlService = accessControlService;
            _identityManagementService = identityManagementService;
        }

        public long CurrentUserId =>
            _currentUser.IsAuthenticated && _currentUser.UserId.HasValue
                ? _currentUser.UserId.Value
                : throw new ForbiddenAccessException("Authenticated user context is required.");

        public async Task<bool> HasGlobalManageAsync(CancellationToken cancellationToken)
        {
            return _currentUser.IsInRole("Admin") ||
                   await _accessControlService.HasGlobalPermissionAsync(CurrentUserId, "admin.access", cancellationToken) ||
                   await _accessControlService.HasGlobalPermissionAsync(CurrentUserId, "classification.acl.manage", cancellationToken);
        }

        public async Task EnsureHasPermissionAsync(string permissionKey, CancellationToken cancellationToken)
        {
            if (_currentUser.IsInRole("Admin") ||
                _currentUser.IsInRole("Superuser") ||
                await _identityManagementService.HasUserPermissionAsync(CurrentUserId, permissionKey, cancellationToken))
            {
                return;
            }

            throw new ForbiddenAccessException($"User lacks required permission '{permissionKey}'.");
        }

        public async Task EnsureCanReadAsync(long classificationId, CancellationToken cancellationToken)
        {
            if (await HasGlobalManageAsync(cancellationToken) ||
                await _accessControlService.CanReadClassificationAsync(CurrentUserId, classificationId, cancellationToken))
            {
                return;
            }

            throw new ForbiddenAccessException("User is not allowed to read this classification.");
        }

        public async Task EnsureCanWriteAsync(long classificationId, CancellationToken cancellationToken)
        {
            if (await HasGlobalManageAsync(cancellationToken) ||
                await _accessControlService.CanWriteClassificationAsync(CurrentUserId, classificationId, cancellationToken))
            {
                return;
            }

            throw new ForbiddenAccessException("شما مجوز ایجاد زیرپوشه در این طبقه‌بندی را ندارید.");
        }

        public async Task EnsureCanManageAsync(long classificationId, CancellationToken cancellationToken)
        {
            if (await HasGlobalManageAsync(cancellationToken) ||
                await _accessControlService.CanManageClassificationAsync(CurrentUserId, classificationId, cancellationToken) ||
                await _accessControlService.CanWriteClassificationAsync(CurrentUserId, classificationId, cancellationToken))
            {
                return;
            }

            throw new ForbiddenAccessException("شما مجوز ویرایش یا مدیریت این طبقه‌بندی را ندارید.");
        }

        public async Task<HashSet<long>> GetAccessibleSetAsync(
            IReadOnlyCollection<Classification> allNodes,
            ClassificationPermissionMask requiredMask,
            CancellationToken cancellationToken)
        {
            if (await HasGlobalManageAsync(cancellationToken))
            {
                return allNodes.Select(node => node.Id).ToHashSet();
            }

            var allowedIds = await _accessControlService.GetAccessibleClassificationIdsAsync(
                CurrentUserId,
                requiredMask,
                cancellationToken);

            return allowedIds.ToHashSet();
        }

        public void InvalidateAllCaches()
        {
            _accessControlService.InvalidateAllCaches();
        }
    }
}
