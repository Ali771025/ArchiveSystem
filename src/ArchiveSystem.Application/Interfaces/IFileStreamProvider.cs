// src/ArchiveSystem.Application/Interfaces/IFileStreamProvider.cs
namespace ArchiveSystem.Application.Interfaces
{
    public interface IFileStreamProvider
    {
        Task<Stream> ReadFileStreamAsync(string filePath, CancellationToken cancellationToken = default);
        Task<string> WriteFileStreamAsync(Stream fileStream, string fileName, string contentType, long caseId, CancellationToken cancellationToken = default);
        Task<bool> DeleteFileStreamAsync(long fileId, CancellationToken cancellationToken = default);
        Task<byte[]> ReadThumbnailAsync(long fileId, CancellationToken cancellationToken = default);
        Task<bool> WriteThumbnailAsync(long fileId, byte[] thumbnail, CancellationToken cancellationToken = default);
        Task<string> ReadOcrResultAsync(long fileId, CancellationToken cancellationToken = default);
        Task<bool> WriteOcrResultAsync(long fileId, string ocrResult, CancellationToken cancellationToken = default);
    }
}
