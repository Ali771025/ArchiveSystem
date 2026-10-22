// src/ArchiveSystem.Domain/IServices/IStorageService.cs
namespace ArchiveSystem.Domain.Services
{
    public interface IStorageService
    {
        Task UploadStreamAsync(string bucket, string objectName, Stream stream, CancellationToken cancellationToken = default);
        Task<Stream> GetObjectStreamAsync(string bucket, string objectName, CancellationToken cancellationToken = default);
        Task<byte[]> GetObjectBytesAsync(string bucket, string objectName, CancellationToken cancellationToken = default);
        Task<string> GetPresignedDownloadUrlAsync(string bucket, string objectName, TimeSpan expiration, CancellationToken cancellationToken = default);
        Task<bool> ObjectExistsAsync(string bucket, string objectName, CancellationToken cancellationToken = default);
        Task DeleteObjectAsync(string bucket, string objectName, CancellationToken cancellationToken = default);
        Task<long> GetObjectSizeAsync(string bucket, string objectName, CancellationToken cancellationToken = default);
    }
}
