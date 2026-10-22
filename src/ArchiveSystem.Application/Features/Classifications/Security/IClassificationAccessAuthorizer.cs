using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;

namespace ArchiveSystem.Application.Features.Classifications.Security
{
    public interface IClassificationAccessAuthorizer
    {
        long CurrentUserId { get; }
        Task<bool> HasGlobalManageAsync(CancellationToken cancellationToken);
        Task EnsureHasPermissionAsync(string permissionKey, CancellationToken cancellationToken);
        Task EnsureCanReadAsync(long classificationId, CancellationToken cancellationToken);
        Task EnsureCanWriteAsync(long classificationId, CancellationToken cancellationToken);
        Task EnsureCanManageAsync(long classificationId, CancellationToken cancellationToken);
        Task<HashSet<long>> GetAccessibleSetAsync(
            IReadOnlyCollection<Classification> allNodes,
            ClassificationPermissionMask requiredMask,
            CancellationToken cancellationToken);
        void InvalidateAllCaches();
    }
}
