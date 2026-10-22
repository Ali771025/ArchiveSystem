// src/ArchiveSystem.Infrastructure/Services/MinioStorageService.cs
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using ArchiveSystem.Domain.Services;

namespace ArchiveSystem.Infrastructure.Services
{
    public class MinioStorageService : IStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public MinioStorageService(IAmazonS3 s3Client, string bucketName)
        {
            _s3Client = s3Client;
            _bucketName = bucketName;
        }

        public async Task UploadStreamAsync(string bucket, string objectName, Stream stream, CancellationToken cancellationToken = default)
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = bucket ?? _bucketName,
                Key = objectName,
                InputStream = stream,
                ContentType = GetContentType(objectName)
            };

            var response = await _s3Client.PutObjectAsync(putRequest, cancellationToken);
            
            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new InvalidOperationException($"Failed to upload object {objectName}. Status: {response.HttpStatusCode}");
            }
        }

        public async Task<Stream> GetObjectStreamAsync(string bucket, string objectName, CancellationToken cancellationToken = default)
        {
            var getRequest = new GetObjectRequest
            {
                BucketName = bucket ?? _bucketName,
                Key = objectName
            };

            var response = await _s3Client.GetObjectAsync(getRequest, cancellationToken);
            
            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new FileNotFoundException($"Object {objectName} not found in bucket {bucket ?? _bucketName}");
            }

            return response.ResponseStream;
        }

        public async Task<byte[]> GetObjectBytesAsync(string bucket, string objectName, CancellationToken cancellationToken = default)
        {
            using var stream = await GetObjectStreamAsync(bucket, objectName, cancellationToken);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken);
            return memoryStream.ToArray();
        }

        public async Task<string> GetPresignedDownloadUrlAsync(string bucket, string objectName, TimeSpan expiration, CancellationToken cancellationToken = default)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucket ?? _bucketName,
                Key = objectName,
                Expires = DateTime.UtcNow.Add(expiration),
                Protocol = Protocol.HTTPS,
                Verb = HttpVerb.GET
            };

            return _s3Client.GetPreSignedURL(request);
        }

        public async Task<bool> ObjectExistsAsync(string bucket, string objectName, CancellationToken cancellationToken = default)
        {
            try
            {
                var headRequest = new GetObjectMetadataRequest
                {
                    BucketName = bucket ?? _bucketName,
                    Key = objectName
                };

                var response = await _s3Client.GetObjectMetadataAsync(headRequest, cancellationToken);
                return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        public async Task DeleteObjectAsync(string bucket, string objectName, CancellationToken cancellationToken = default)
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = bucket ?? _bucketName,
                Key = objectName
            };

            var response = await _s3Client.DeleteObjectAsync(deleteRequest, cancellationToken);
            
            if (response.HttpStatusCode != System.Net.HttpStatusCode.NoContent)
            {
                throw new InvalidOperationException($"Failed to delete object {objectName}. Status: {response.HttpStatusCode}");
            }
        }

        public async Task<long> GetObjectSizeAsync(string bucket, string objectName, CancellationToken cancellationToken = default)
        {
            var headRequest = new GetObjectMetadataRequest
            {
                BucketName = bucket ?? _bucketName,
                Key = objectName
            };

            var response = await _s3Client.GetObjectMetadataAsync(headRequest, cancellationToken);
            return response.ContentLength;
        }

        private static string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".txt" => "text/plain",
                ".html" or ".htm" => "text/html",
                ".json" => "application/json",
                ".xml" => "application/xml",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                ".mp3" => "audio/mpeg",
                ".mp4" => "video/mp4",
                ".avi" => "video/x-msvideo",
                _ => "application/octet-stream"
            };
        }
    }
}