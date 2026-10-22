using ArchiveSystem.Domain.Entities;

namespace ArchiveSystem.Application.Interfaces.Persistence
{
    public interface IFileVersionRepository
    {
        Task<FileVersion> CreateVersionAsync(FileVersion fileVersion, CancellationToken cancellationToken = default);
        Task<FileVersion?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<FileVersion?> GetLatestVersionAsync(long fileId, CancellationToken cancellationToken = default);
        Task<FileVersion?> GetLatestVersionWithThumbnailAsync(long fileId, CancellationToken cancellationToken = default);
        Task<FileVersion?> GetLatestVersionWithOcrAsync(long fileId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<FileVersion>> ListVersionsAsync(long fileId, CancellationToken cancellationToken = default);
        Task<int> GetLatestVersionNumberAsync(long fileId, CancellationToken cancellationToken = default);
        Task UpdateAsync(FileVersion fileVersion, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<long>> GetPendingThumbnailVersionIdsAsync(int batchSize, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<long>> GetPendingOcrVersionIdsAsync(int batchSize, CancellationToken cancellationToken = default);
        Task<int> ResetStaleOcrProcessingAsync(
            TimeSpan processingTimeout,
            string error,
            CancellationToken cancellationToken = default);
        Task<bool> TryStartThumbnailProcessingAsync(long fileVersionId, CancellationToken cancellationToken = default);
        Task<bool> TryStartOcrProcessingAsync(long fileVersionId, CancellationToken cancellationToken = default);
        Task<bool> TryCompleteThumbnailAsync(
            long fileVersionId,
            byte[] thumbnail,
            string thumbnailContentType,
            int width,
            int height,
            CancellationToken cancellationToken = default);
        Task<bool> TryFailThumbnailAsync(long fileVersionId, string error, CancellationToken cancellationToken = default);
        Task<bool> TryCompleteOcrAsync(
            long fileVersionId,
            string ocrJson,
            string ocrText,
            string language,
            int wordCount,
            CancellationToken cancellationToken = default);
        Task<bool> TryFailOcrAsync(long fileVersionId, string error, CancellationToken cancellationToken = default);
        Task<bool> TryQueueOcrRetryAsync(long fileVersionId, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
