using ArchiveSystem.Domain.Enums;

namespace ArchiveSystem.Application.Common.Interfaces.Security
{
    public interface IAuthorizationCache
    {
        Task<IReadOnlyDictionary<long, ClassificationPermissionMask>> GetOrCreateClassificationPermissionsAsync(
            long userId,
            Func<Task<IReadOnlyDictionary<long, ClassificationPermissionMask>>> factory,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<long>> GetOrCreateRolesAsync(
            long userId,
            Func<Task<IReadOnlyCollection<long>>> factory,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<long, TagPermissionMask>> GetOrCreateTagPermissionsAsync(
            long userId,
            Func<Task<IReadOnlyDictionary<long, TagPermissionMask>>> factory,
            CancellationToken cancellationToken = default);

        void InvalidateUser(long userId);
        void InvalidateAll();
    }
}
