using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;

namespace ArchiveSystem.Application.AccessControl
{
    public interface IClassificationAclRepository
    {
        Task<IReadOnlyList<ClassificationAclEntry>> GetByClassificationIdAsync(long classificationId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ClassificationAclEntry>> GetByClassificationIdAndDescendantsAsync(long classificationId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ClassificationAclEntry>> GetBySubjectsAsync(
            long userId,
            IReadOnlyCollection<long> roleIds,
            CancellationToken cancellationToken = default);

        Task GrantUserAccessAsync(
            long userId,
            long classificationId,
            ClassificationPermissionMask permissionMask,
            CancellationToken cancellationToken = default);

        Task ReplaceForClassificationAsync(
            long classificationId,
            IReadOnlyCollection<ClassificationAclEntry> entries,
            CancellationToken cancellationToken = default);

        Task ReplaceForClassificationAndDescendantsAsync(
            long classificationId,
            IReadOnlyCollection<ClassificationAclEntry> entries,
            CancellationToken cancellationToken = default);

        Task OverrideForDescendantsAsync(
            long classificationId,
            IReadOnlyCollection<ClassificationAclEntry> entries,
            CancellationToken cancellationToken = default);
    }
}
