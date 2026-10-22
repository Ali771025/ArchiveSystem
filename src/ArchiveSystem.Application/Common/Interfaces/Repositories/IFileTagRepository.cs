namespace ArchiveSystem.Application.Common.Interfaces.Repositories
{
    public interface IFileTagRepository
    {
        Task<IReadOnlyCollection<long>> GetTagIdsByFileIdAsync(long fileId, CancellationToken cancellationToken = default);
        Task SetFileTagsAsync(long fileId, IReadOnlyCollection<long> tagIds, CancellationToken cancellationToken = default);
    }
}
