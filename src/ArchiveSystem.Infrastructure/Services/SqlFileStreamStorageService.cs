using ArchiveSystem.Application.Interfaces.Storage;
using ArchiveSystem.Domain.Services;

namespace ArchiveSystem.Infrastructure.Services
{
    public class SqlFileStreamStorageService : IStorageService
    {
        private readonly IObjectStorage _objectStorage;

        public SqlFileStreamStorageService(IObjectStorage objectStorage)
        {
            _objectStorage = objectStorage;
        }

        public Task UploadStreamAsync(string bucket, string objectName, Stream stream, CancellationToken cancellationToken = default)
        {
            return _objectStorage.PutObjectAsync(bucket, objectName, stream, "application/octet-stream", cancellationToken);
        }

        public Task<Stream> GetObjectStreamAsync(string bucket, string objectName, CancellationToken cancellationToken = default)
        {
            return _objectStorage.GetObjectStreamAsync(bucket, objectName, cancellationToken);
        }

        public async Task<byte[]> GetObjectBytesAsync(string bucket, string objectName, CancellationToken cancellationToken = default)
        {
            await using var stream = await _objectStorage.GetObjectStreamAsync(bucket, objectName, cancellationToken);
            await using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, cancellationToken);
            return memory.ToArray();
        }

        public Task<string> GetPresignedDownloadUrlAsync(string bucket, string objectName, TimeSpan expiration, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Presigned URLs are not supported for SqlFileStream provider.");
        }

        public Task<bool> ObjectExistsAsync(string bucket, string objectName, CancellationToken cancellationToken = default)
        {
            return _objectStorage.ExistsAsync(bucket, objectName, cancellationToken);
        }

        public Task DeleteObjectAsync(string bucket, string objectName, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("DeleteObjectAsync is not implemented for SqlFileStream provider.");
        }

        public async Task<long> GetObjectSizeAsync(string bucket, string objectName, CancellationToken cancellationToken = default)
        {
            var stat = await _objectStorage.GetObjectStatAsync(bucket, objectName, cancellationToken);
            return stat.SizeBytes;
        }
    }
}
