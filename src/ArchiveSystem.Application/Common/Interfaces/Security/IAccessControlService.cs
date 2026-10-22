using ArchiveSystem.Domain.Enums;

namespace ArchiveSystem.Application.Common.Interfaces.Security
{
    public interface IAccessControlService
    {
        Task<IReadOnlyCollection<long>> EffectiveRolesAsync(long userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyDictionary<long, ClassificationPermissionMask>> EffectiveClassificationPermissionsAsync(long userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyDictionary<long, TagPermissionMask>> EffectiveTagPermissionsAsync(long userId, CancellationToken cancellationToken = default);

        Task<bool> HasGlobalPermissionAsync(long userId, string permissionKey, CancellationToken cancellationToken = default);
        Task<bool> CanReadClassificationAsync(long userId, long classificationId, CancellationToken cancellationToken = default);
        Task<bool> CanWriteClassificationAsync(long userId, long classificationId, CancellationToken cancellationToken = default);
        Task<bool> CanMoveClassificationAsync(long userId, long classificationId, CancellationToken cancellationToken = default);
        Task<bool> CanManageClassificationAsync(long userId, long classificationId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<long>> GetAccessibleClassificationIdsAsync(long userId, ClassificationPermissionMask requiredMask, CancellationToken cancellationToken = default);

        Task<bool> CanViewFileAsync(long userId, long fileId, CancellationToken cancellationToken = default);
        Task<bool> CanUploadFileVersionAsync(long userId, long classificationId, CancellationToken cancellationToken = default);
        Task<bool> CanAssignTagAsync(long userId, long tagId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<long>> GetAllowedTagIdsAsync(long userId, TagPermissionMask requiredMask, CancellationToken cancellationToken = default);

        void InvalidateUserCache(long userId);
        void InvalidateAllCaches();
    }
}
