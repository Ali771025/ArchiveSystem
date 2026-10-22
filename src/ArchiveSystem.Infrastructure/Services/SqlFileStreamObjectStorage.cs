using ArchiveSystem.Application.Interfaces.Storage;
using ArchiveSystem.Application.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace ArchiveSystem.Infrastructure.Services
{
    public class SqlFileStreamObjectStorage : IObjectStorage, IFileContentStore
    {
        private readonly string _connectionString;
        private readonly SqlFileStreamOptions _options;

        public SqlFileStreamObjectStorage(IConfiguration configuration, IOptions<SqlFileStreamOptions> options)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            _options = options.Value;
        }

        public Task EnsureBucketExistsAsync(string bucketName, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<string> InitiateMultipartUploadAsync(string bucketName, string objectKey, string contentType, CancellationToken cancellationToken = default)
        {
            var uploadId = Guid.NewGuid().ToString("N");
            return Task.FromResult(uploadId);
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
            if (partNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(partNumber));
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var bytes = await ReadAllBytesAsync(content, cancellationToken);
            var partKey = BuildPartKey(objectKey, uploadId, partNumber);

            await UpsertBlobAsync(connection, partKey, bytes, "application/octet-stream", cancellationToken);

            var etag = ComputeSha256Hex(bytes);
            return new UploadPartResult(partNumber, etag);
        }

        public async Task CompleteMultipartUploadAsync(
            string bucketName,
            string objectKey,
            string uploadId,
            IReadOnlyCollection<MultipartPartETag> parts,
            CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var transaction = connection.BeginTransaction();

            try
            {
                await using var merged = new MemoryStream();
                foreach (var part in parts.OrderBy(p => p.PartNumber))
                {
                    var partKey = BuildPartKey(objectKey, uploadId, part.PartNumber);
                    var partBytes = await ReadBlobAsync(connection, transaction, partKey, cancellationToken);
                    if (partBytes == null)
                    {
                        throw new InvalidOperationException($"Upload part '{part.PartNumber}' not found for '{objectKey}'.");
                    }

                    await merged.WriteAsync(partBytes, cancellationToken);
                }

                merged.Position = 0;
                var mergedBytes = merged.ToArray();
                await UpsertBlobAsync(connection, transaction, objectKey, mergedBytes, "application/octet-stream", cancellationToken);

                foreach (var part in parts)
                {
                    var partKey = BuildPartKey(objectKey, uploadId, part.PartNumber);
                    await DeleteBlobAsync(connection, transaction, partKey, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task AbortMultipartUploadAsync(string bucketName, string objectKey, string uploadId, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(
                @"DELETE FROM dbo.FileBlobs WHERE BlobKey LIKE @Prefix",
                connection);
            command.Parameters.AddWithValue("@Prefix", BuildPartPrefix(objectKey, uploadId) + "%");
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<Stream> GetObjectStreamAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var bytes = await ReadBlobAsync(connection, null, objectKey, cancellationToken);
            if (bytes == null)
            {
                throw new FileNotFoundException($"Object '{objectKey}' not found.");
            }

            return new MemoryStream(bytes, writable: false);
        }

        public async Task<ObjectStat> GetObjectStatAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(
                @"SELECT DATALENGTH(Content), ContentType
                  FROM dbo.FileBlobs
                  WHERE BlobKey = @BlobKey",
                connection);
            command.Parameters.AddWithValue("@BlobKey", objectKey);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new FileNotFoundException($"Object '{objectKey}' not found.");
            }

            var size = reader.IsDBNull(0) ? 0 : reader.GetInt64(0);
            var contentType = reader.IsDBNull(1) ? null : reader.GetString(1);
            return new ObjectStat(size, contentType);
        }

        public async Task PutObjectAsync(string bucketName, string objectKey, Stream content, string contentType, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var bytes = await ReadAllBytesAsync(content, cancellationToken);
            await UpsertBlobAsync(connection, objectKey, bytes, contentType, cancellationToken);
        }

        public async Task<bool> ExistsAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(
                @"SELECT COUNT(1) FROM dbo.FileBlobs WHERE BlobKey = @BlobKey",
                connection);
            command.Parameters.AddWithValue("@BlobKey", objectKey);

            var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            return count > 0;
        }

        public async Task<string> ComputeSha256Async(string bucketName, string objectKey, CancellationToken cancellationToken = default)
        {
            await using var stream = await GetObjectStreamAsync(bucketName, objectKey, cancellationToken);
            using var sha256 = SHA256.Create();
            var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private async Task UpsertBlobAsync(SqlConnection connection, string objectKey, byte[] bytes, string contentType, CancellationToken cancellationToken)
        {
            await UpsertBlobAsync(connection, null, objectKey, bytes, contentType, cancellationToken);
        }

        private async Task UpsertBlobAsync(
            SqlConnection connection,
            SqlTransaction? transaction,
            string objectKey,
            byte[] bytes,
            string contentType,
            CancellationToken cancellationToken)
        {
            using var command = new SqlCommand(
                @"MERGE dbo.FileBlobs AS target
                  USING (SELECT @BlobKey AS BlobKey) AS source
                  ON target.BlobKey = source.BlobKey
                  WHEN MATCHED THEN
                    UPDATE SET Content = @Content, ContentType = @ContentType, UpdatedAt = SYSUTCDATETIME()
                  WHEN NOT MATCHED THEN
                    INSERT (BlobKey, Content, ContentType, CreatedAt, UpdatedAt)
                    VALUES (@BlobKey, @Content, @ContentType, SYSUTCDATETIME(), SYSUTCDATETIME());",
                connection,
                transaction);

            command.Parameters.AddWithValue("@BlobKey", objectKey);
            command.Parameters.AddWithValue("@Content", bytes);
            command.Parameters.AddWithValue("@ContentType", string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task<byte[]?> ReadBlobAsync(
            SqlConnection connection,
            SqlTransaction? transaction,
            string objectKey,
            CancellationToken cancellationToken)
        {
            using var command = new SqlCommand(
                @"SELECT Content FROM dbo.FileBlobs WHERE BlobKey = @BlobKey",
                connection,
                transaction);
            command.Parameters.AddWithValue("@BlobKey", objectKey);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result is DBNull || result == null)
            {
                return null;
            }

            return (byte[])result;
        }

        private async Task DeleteBlobAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            string objectKey,
            CancellationToken cancellationToken)
        {
            using var command = new SqlCommand(
                @"DELETE FROM dbo.FileBlobs WHERE BlobKey = @BlobKey",
                connection,
                transaction);
            command.Parameters.AddWithValue("@BlobKey", objectKey);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken cancellationToken)
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            await using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, cancellationToken);
            return memory.ToArray();
        }

        private static string ComputeSha256Hex(byte[] bytes)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private string BuildPartKey(string objectKey, string uploadId, int partNumber)
        {
            var prefix = _options.ContainerPath.Trim().Trim('/');
            return $"{prefix}/parts/{uploadId}/{objectKey.Replace('\\', '/').TrimStart('/')}.part.{partNumber:D8}";
        }

        private string BuildPartPrefix(string objectKey, string uploadId)
        {
            var prefix = _options.ContainerPath.Trim().Trim('/');
            return $"{prefix}/parts/{uploadId}/{objectKey.Replace('\\', '/').TrimStart('/')}.part.";
        }
    }
}
