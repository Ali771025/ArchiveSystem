using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Infrastructure.Repositories
{
    public class EfFileVersionRepository : IFileVersionRepository
    {
        private readonly ArchiveDbContext _dbContext;

        public EfFileVersionRepository(ArchiveDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<FileVersion> CreateVersionAsync(FileVersion fileVersion, CancellationToken cancellationToken = default)
        {
            await _dbContext.FileVersions.AddAsync(fileVersion, cancellationToken);
            return fileVersion;
        }

        public async Task<FileVersion?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.FileVersions
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        }

        public async Task<FileVersion?> GetLatestVersionAsync(long fileId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.FileVersions
                .AsNoTracking()
                .Where(v => v.FileId == fileId && !v.DeletedAt.HasValue)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<FileVersion?> GetLatestVersionWithThumbnailAsync(long fileId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.FileVersions
                .AsNoTracking()
                .Where(v =>
                    v.FileId == fileId &&
                    !v.DeletedAt.HasValue &&
                    v.Thumbnail != null &&
                    v.Thumbnail.Length > 0)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<FileVersion?> GetLatestVersionWithOcrAsync(long fileId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.FileVersions
                .AsNoTracking()
                .Where(v =>
                    v.FileId == fileId &&
                    !v.DeletedAt.HasValue &&
                    !string.IsNullOrWhiteSpace(v.OcrResult))
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<FileVersion>> ListVersionsAsync(long fileId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.FileVersions
                .AsNoTracking()
                .Where(v => v.FileId == fileId)
                .OrderByDescending(v => v.VersionNumber)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetLatestVersionNumberAsync(long fileId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.FileVersions
                .Where(v => v.FileId == fileId)
                .Select(v => (int?)v.VersionNumber)
                .MaxAsync(cancellationToken) ?? 0;
        }

        public Task UpdateAsync(FileVersion fileVersion, CancellationToken cancellationToken = default)
        {
            _dbContext.FileVersions.Update(fileVersion);
            return Task.CompletedTask;
        }

        public async Task<IReadOnlyList<long>> GetPendingThumbnailVersionIdsAsync(int batchSize, CancellationToken cancellationToken = default)
        {
            var size = NormalizeBatchSize(batchSize);
            return await _dbContext.FileVersions
                .AsNoTracking()
                .Where(v =>
                    !v.DeletedAt.HasValue &&
                    v.ThumbnailStatus == ProcessingStatus.Pending &&
                    (v.Thumbnail == null || v.Thumbnail.Length == 0))
                .OrderBy(v => v.CreatedAt)
                .Select(v => v.Id)
                .Take(size)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<long>> GetPendingOcrVersionIdsAsync(int batchSize, CancellationToken cancellationToken = default)
        {
            var size = NormalizeBatchSize(batchSize);
            return await _dbContext.FileVersions
                .AsNoTracking()
                .Where(v => !v.DeletedAt.HasValue && v.OcrStatus == ProcessingStatus.Pending)
                .OrderBy(v => v.CreatedAt)
                .Select(v => v.Id)
                .Take(size)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> ResetStaleOcrProcessingAsync(
            TimeSpan processingTimeout,
            string error,
            CancellationToken cancellationToken = default)
        {
            if (processingTimeout <= TimeSpan.Zero)
            {
                return 0;
            }

            var now = DateTime.UtcNow;
            var staleBefore = now - processingTimeout;
            var trimmedError = TrimError(error);

            return await _dbContext.FileVersions
                .Where(v =>
                    !v.DeletedAt.HasValue &&
                    v.OcrStatus == ProcessingStatus.Processing &&
                    ((v.ModifiedAt.HasValue && v.ModifiedAt.Value < staleBefore) ||
                     (!v.ModifiedAt.HasValue && v.CreatedAt < staleBefore)))
                .ExecuteUpdateAsync(updates => updates
                    .SetProperty(v => v.OcrStatus, ProcessingStatus.Pending)
                    .SetProperty(v => v.OcrError, trimmedError)
                    .SetProperty(v => v.ModifiedAt, now), cancellationToken);
        }

        public async Task<bool> TryStartThumbnailProcessingAsync(long fileVersionId, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var affected = await _dbContext.FileVersions
                .Where(v =>
                    v.Id == fileVersionId &&
                    !v.DeletedAt.HasValue &&
                    (v.Thumbnail == null || v.Thumbnail.Length == 0) &&
                    v.ThumbnailStatus != ProcessingStatus.Processing &&
                    v.ThumbnailStatus != ProcessingStatus.Completed)
                .ExecuteUpdateAsync(updates => updates
                    .SetProperty(v => v.ThumbnailStatus, ProcessingStatus.Processing)
                    .SetProperty(v => v.ThumbnailError, (string?)null)
                    .SetProperty(v => v.ModifiedAt, now), cancellationToken);

            return affected == 1;
        }

        public async Task<bool> TryStartOcrProcessingAsync(long fileVersionId, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var affected = await _dbContext.FileVersions
                .Where(v =>
                    v.Id == fileVersionId &&
                    !v.DeletedAt.HasValue &&
                    v.OcrStatus != ProcessingStatus.Processing &&
                    v.OcrStatus != ProcessingStatus.Completed)
                .ExecuteUpdateAsync(updates => updates
                    .SetProperty(v => v.OcrStatus, ProcessingStatus.Processing)
                    .SetProperty(v => v.OcrError, (string?)null)
                    .SetProperty(v => v.ModifiedAt, now), cancellationToken);

            return affected == 1;
        }

        public async Task<bool> TryCompleteThumbnailAsync(
            long fileVersionId,
            byte[] thumbnail,
            string thumbnailContentType,
            int width,
            int height,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var affected = await _dbContext.FileVersions
                .Where(v =>
                    v.Id == fileVersionId &&
                    !v.DeletedAt.HasValue &&
                    v.ThumbnailStatus == ProcessingStatus.Processing)
                .ExecuteUpdateAsync(updates => updates
                    .SetProperty(v => v.Thumbnail, thumbnail)
                    .SetProperty(v => v.ThumbnailContentType, thumbnailContentType)
                    .SetProperty(v => v.ThumbnailWidth, width)
                    .SetProperty(v => v.ThumbnailHeight, height)
                    .SetProperty(v => v.ThumbnailStatus, ProcessingStatus.Completed)
                    .SetProperty(v => v.ThumbnailError, (string?)null)
                    .SetProperty(v => v.ModifiedAt, now), cancellationToken);

            return affected == 1;
        }

        public async Task<bool> TryFailThumbnailAsync(long fileVersionId, string error, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var trimmedError = TrimError(error);
            var affected = await _dbContext.FileVersions
                .Where(v =>
                    v.Id == fileVersionId &&
                    !v.DeletedAt.HasValue &&
                    v.ThumbnailStatus == ProcessingStatus.Processing)
                .ExecuteUpdateAsync(updates => updates
                    .SetProperty(v => v.ThumbnailStatus, ProcessingStatus.Failed)
                    .SetProperty(v => v.ThumbnailError, trimmedError)
                    .SetProperty(v => v.ModifiedAt, now), cancellationToken);

            return affected == 1;
        }

        public async Task<bool> TryCompleteOcrAsync(
            long fileVersionId,
            string ocrJson,
            string ocrText,
            string language,
            int wordCount,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var affected = await _dbContext.FileVersions
                .Where(v =>
                    v.Id == fileVersionId &&
                    !v.DeletedAt.HasValue &&
                    v.OcrStatus == ProcessingStatus.Processing)
                .ExecuteUpdateAsync(updates => updates
                    .SetProperty(v => v.OcrResult, ocrJson)
                    .SetProperty(v => v.OcrText, ocrText)
                    .SetProperty(v => v.OcrLanguage, language)
                    .SetProperty(v => v.OcrWordCount, wordCount)
                    .SetProperty(v => v.OcrStatus, ProcessingStatus.Completed)
                    .SetProperty(v => v.OcrError, (string?)null)
                    .SetProperty(v => v.ModifiedAt, now), cancellationToken);

            return affected == 1;
        }

        public async Task<bool> TryQueueOcrRetryAsync(
            long fileVersionId,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var affected = await _dbContext.FileVersions
                .Where(v =>
                    v.Id == fileVersionId &&
                    !v.DeletedAt.HasValue &&
                    v.OcrStatus == ProcessingStatus.Failed)
                .ExecuteUpdateAsync(updates => updates
                    .SetProperty(v => v.OcrStatus, ProcessingStatus.Pending)
                    .SetProperty(v => v.OcrError, (string?)null)
                    .SetProperty(v => v.OcrResult, (string?)null)
                    .SetProperty(v => v.OcrText, (string?)null)
                    .SetProperty(v => v.OcrLanguage, (string?)null)
                    .SetProperty(v => v.OcrWordCount, (int?)null)
                    .SetProperty(v => v.ModifiedAt, now), cancellationToken);

            return affected == 1;
        }

        public async Task<bool> TryFailOcrAsync(long fileVersionId, string error, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var trimmedError = TrimError(error);
            var affected = await _dbContext.FileVersions
                .Where(v =>
                    v.Id == fileVersionId &&
                    !v.DeletedAt.HasValue &&
                    v.OcrStatus == ProcessingStatus.Processing)
                .ExecuteUpdateAsync(updates => updates
                    .SetProperty(v => v.OcrStatus, ProcessingStatus.Failed)
                    .SetProperty(v => v.OcrError, trimmedError)
                    .SetProperty(v => v.ModifiedAt, now), cancellationToken);

            return affected == 1;
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private static int NormalizeBatchSize(int batchSize)
        {
            if (batchSize <= 0)
            {
                return 100;
            }

            return Math.Min(batchSize, 1000);
        }

        private static string TrimError(string? error)
        {
            if (string.IsNullOrWhiteSpace(error))
            {
                return "Unknown processing error";
            }

            return error.Length <= 512 ? error : error[..512];
        }
    }
}
