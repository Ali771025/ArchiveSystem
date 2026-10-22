// src/ArchiveSystem.Domain/IServices/ISearchService.cs
namespace ArchiveSystem.Domain.Services
{
    public interface ISearchService
    {
        Task IndexFileAsync(long fileId, string content, CancellationToken cancellationToken = default);
        Task UpdateFileIndexAsync(long fileId, string content, CancellationToken cancellationToken = default);
        Task DeleteFileIndexAsync(long fileId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<(long FileId, float Score)>> SearchAsync(string query, int limit = 10, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<(long FileId, float Score)>> SearchByMetadataAsync(IDictionary<string, string> metadata, int limit = 10, CancellationToken cancellationToken = default);
    }
}