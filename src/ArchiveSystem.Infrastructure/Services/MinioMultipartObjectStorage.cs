using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using ArchiveSystem.Application.Interfaces.Storage;
using System.Security.Cryptography;

namespace ArchiveSystem.Infrastructure.Services
{
    public class MinioMultipartObjectStorage : IObjectStorage, IFileContentStore
    {
        private readonly IAmazonS3 _s3Client;

        public MinioMultipartObjectStorage(IAmazonS3 s3Client)
        {
            _s3Client = s3Client;
        }

        public async Task EnsureBucketExistsAsync(string bucketName, CancellationToken cancellationToken = default)
        {
            var exists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName);
            if (exists)
            {
                return;
            }

            await _s3Client.PutBucketAsync(new PutBucketRequest
            {
                BucketName = bucketName,
                UseClientRegion = true
            }, cancellationToken);
        }

        public async Task<string> InitiateMultipartUploadAsync(string bucketName, string objectKey, string contentType, CancellationToken cancellationToken = default)
        {
            var response = await _s3Client.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest
            {
                BucketName = bucketName,
                Key = objectKey,
                ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType
            }, cancellationToken);

            return response.UploadId;
        }

        public async Task<UploadPartResult> UploadPartAsync(
            string bucketName,
            string objectKey,
            string uploadId,
            int partNumber,
            Stream content,
            long contentLength,
            CancellationToken cancellationToken = default)
        {
            var uploadStream = content;
            MemoryStream? bufferedStream = null;

            if (!content.CanSeek)
            {
                var capacity = contentLength > 0 && contentLength <= int.MaxValue ? (int)contentLength : 0;
                bufferedStream = capacity > 0 ? new MemoryStream(capacity) : new MemoryStream();
                await content.CopyToAsync(bufferedStream, cancellationToken);
                bufferedStream.Position = 0;
                uploadStream = bufferedStream;
            }
            else if (content.Position != 0)
            {
                content.Position = 0;
            }

            var partSize = bufferedStream?.Length ?? contentLength;

            try
            {
                var response = await _s3Client.UploadPartAsync(new UploadPartRequest
                {
                    BucketName = bucketName,
                    Key = objectKey,
                    UploadId = uploadId,
                    PartNumber = partNumber,
                    InputStream = uploadStream,
                    PartSize = partSize
                }, cancellationToken);

                return new UploadPartResult(partNumber, NormalizeEtag(response.ETag));
            }
            finally
            {
                bufferedStream?.Dispose();
            }
        }

        public async Task CompleteMultipartUploadAsync(
            string bucketName,
            string objectKey,
            string uploadId,
            IReadOnlyCollection<MultipartPartETag> parts,
            CancellationToken cancellationToken = default)
        {
            var request = new CompleteMultipartUploadRequest
            {
                BucketName = bucketName,
                Key = objectKey,
                UploadId = uploadId
            };

            foreach (var part in parts.OrderBy(p => p.PartNumber))
            {
                request.AddPartETags(new PartETag(part.PartNumber, NormalizeEtag(part.ETag)));
            }

            await _s3Client.CompleteMultipartUploadAsync(request, cancellationToken);
        }

        public async Task AbortMultipartUploadAsync(string bucketName, string objectKey, string uploadId, CancellationToken cancellationToken = default)
        {
            await _s3Client.AbortMultipartUploadAsync(new AbortMultipartUploadRequest
            {
                BucketName = bucketName,
                Key = objectKey,
                UploadId = uploadId
            }, cancellationToken);
        }

        public async Task<Stream> GetObjectStreamAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
        {
            var response = await _s3Client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey
            }, cancellationToken);

            return new S3ObjectStream(response);
        }

        public async Task<ObjectStat> GetObjectStatAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
        {
            var metadata = await _s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = bucketName,
                Key = objectKey
            }, cancellationToken);

            return new ObjectStat(metadata.ContentLength, metadata.Headers.ContentType);
        }

        public async Task PutObjectAsync(string bucketName, string objectKey, Stream content, string contentType, CancellationToken cancellationToken = default)
        {
            await _s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey,
                InputStream = content,
                ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType
            }, cancellationToken);
        }

        public async Task<bool> ExistsAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
        {
            try
            {
                await _s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
                {
                    BucketName = bucketName,
                    Key = objectKey
                }, cancellationToken);

                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        public async Task<string> ComputeSha256Async(string bucketName, string objectKey, CancellationToken cancellationToken = default)
        {
            using var stream = await GetObjectStreamAsync(bucketName, objectKey, cancellationToken);
            using var sha256 = SHA256.Create();
            var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static string NormalizeEtag(string etag)
        {
            return etag.Trim('"');
        }

        private sealed class S3ObjectStream : Stream
        {
            private readonly GetObjectResponse _response;
            private readonly Stream _inner;

            public S3ObjectStream(GetObjectResponse response)
            {
                _response = response;
                _inner = response.ResponseStream;
            }

            public override bool CanRead => _inner.CanRead;
            public override bool CanSeek => _inner.CanSeek;
            public override bool CanWrite => _inner.CanWrite;
            public override long Length => _inner.Length;
            public override long Position { get => _inner.Position; set => _inner.Position = value; }
            public override void Flush() => _inner.Flush();
            public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
            public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
            public override void SetLength(long value) => _inner.SetLength(value);
            public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);
            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => _inner.ReadAsync(buffer, cancellationToken);
            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _inner.ReadAsync(buffer, offset, count, cancellationToken);
            public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => _inner.CopyToAsync(destination, bufferSize, cancellationToken);
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _response.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}
