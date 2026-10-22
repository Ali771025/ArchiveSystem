using ArchiveSystem.Domain.Entities;

namespace ArchiveSystem.Application.Common.Interfaces.Repositories
{
    public interface ITagAclRepository
    {
        Task<IReadOnlyList<TagAclEntry>> GetByTagIdAsync(long tagId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TagAclEntry>> GetBySubjectsAsync(
            long userId,
            IReadOnlyCollection<long> roleIds,
            CancellationToken cancellationToken = default);

        Task ReplaceForTagAsync(long tagId, IReadOnlyCollection<TagAclEntry> entries, CancellationToken cancellationToken = default);
    }
}
