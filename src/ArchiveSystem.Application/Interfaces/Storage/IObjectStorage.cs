namespace ArchiveSystem.Application.Interfaces.Storage
{
    public interface IObjectStorage
    {
        Task EnsureBucketExistsAsync(string bucketName, CancellationToken cancellationToken = default);
        Task<string> InitiateMultipartUploadAsync(string bucketName, string objectKey, string contentType, CancellationToken cancellationToken = default);
        Task<UploadPartResult> UploadPartAsync(
            string bucketName,
            string objectKey,
            string uploadId,
            int partNumber,
            Stream content,
            long contentLength,
            CancellationToken cancellationToken = default);
        Task CompleteMultipartUploadAsync(
            string bucketName,
            string objectKey,
            string uploadId,
            IReadOnlyCollection<MultipartPartETag> parts,
            CancellationToken cancellationToken = default);
        Task AbortMultipartUploadAsync(string bucketName, string objectKey, string uploadId, CancellationToken cancellationToken = default);
        Task<Stream> GetObjectStreamAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default);
        Task<ObjectStat> GetObjectStatAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default);
        Task PutObjectAsync(string bucketName, string objectKey, Stream content, string contentType, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default);
    }

    public sealed record MultipartPartETag(int PartNumber, string ETag);
    public sealed record UploadPartResult(int PartNumber, string ETag);
    public sealed record ObjectStat(long SizeBytes, string? ContentType);
}
