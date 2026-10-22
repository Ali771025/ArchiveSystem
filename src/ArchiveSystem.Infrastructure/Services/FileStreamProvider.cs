// src/ArchiveSystem.Infrastructure/Services/FileStreamProvider.cs
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ArchiveSystem.Application.Interfaces;
using ArchiveSystem.Domain.Enums;

namespace ArchiveSystem.Infrastructure.Services
{
    public class FileStreamProvider : IFileStreamProvider
    {
        private readonly string _connectionString;
        private readonly ILogger<FileStreamProvider> _logger;

        public FileStreamProvider(IConfiguration configuration, ILogger<FileStreamProvider> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            _logger = logger;
        }

        public async Task<Stream> ReadFileStreamAsync(string filePath, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(@"
                SELECT Content 
                FROM Files 
                WHERE StoragePath = @Path AND DeletedAt IS NULL", connection);
            command.Parameters.AddWithValue("@Path", filePath);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var content = (byte[])reader["Content"];
                return new MemoryStream(content);
            }

            throw new FileNotFoundException($"File not found: {filePath}");
        }

        public async Task<string> WriteFileStreamAsync(Stream fileStream, string fileName, string contentType, long caseId, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var transaction = connection.BeginTransaction();

            try
            {
                // Read the stream into a byte array
                using var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream, cancellationToken);
                var fileBytes = memoryStream.ToArray();

                var filePath = GenerateFilePath(fileName);
                
                using var command = new SqlCommand(@"
                    INSERT INTO Files (Name, Content, Extension, ContentType, StoragePath, Size, CaseId, CreatedAt, Status) 
                    VALUES (@Name, @Content, @Extension, @ContentType, @StoragePath, @Size, @CaseId, @CreatedAt, @Status);
                    SELECT SCOPE_IDENTITY();", 
                    connection, transaction);

                command.Parameters.AddWithValue("@Name", fileName);
                command.Parameters.AddWithValue("@Content", fileBytes);
                command.Parameters.AddWithValue("@Extension", Path.GetExtension(fileName));
                command.Parameters.AddWithValue("@ContentType", contentType);
                command.Parameters.AddWithValue("@StoragePath", filePath);
                command.Parameters.AddWithValue("@Size", (long)fileBytes.Length);
                command.Parameters.AddWithValue("@CaseId", caseId);
                command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
                command.Parameters.AddWithValue("@Status", (byte)FileStatus.Pending);

                var fileId = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("File saved with ID {FileId} and path {FilePath}", fileId, filePath);

                return filePath;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error saving file {FileName}", fileName);
                throw;
            }
        }

        public async Task<bool> DeleteFileStreamAsync(long fileId, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(@"
                UPDATE Files 
                SET DeletedAt = @DeletedAt, Status = @Status 
                WHERE Id = @Id AND DeletedAt IS NULL", connection);
            command.Parameters.AddWithValue("@Id", fileId);
            command.Parameters.AddWithValue("@DeletedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@Status", (byte)FileStatus.Deleted);

            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
            var success = rowsAffected > 0;

            if (success)
            {
                _logger.LogInformation("File {FileId} marked as deleted", fileId);
            }

            return success;
        }

        public async Task<byte[]> ReadThumbnailAsync(long fileId, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand("SELECT Thumbnail FROM Files WHERE Id = @Id AND DeletedAt IS NULL", connection);
            command.Parameters.AddWithValue("@Id", fileId);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var thumbnail = (byte[])reader["Thumbnail"];
                return thumbnail;
            }

            throw new FileNotFoundException($"Thumbnail not found for file ID: {fileId}");
        }

        public async Task<bool> WriteThumbnailAsync(long fileId, byte[] thumbnail, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand("UPDATE Files SET Thumbnail = @Thumbnail, ModifiedAt = @ModifiedAt WHERE Id = @Id AND DeletedAt IS NULL", connection);
            command.Parameters.AddWithValue("@Id", fileId);
            command.Parameters.AddWithValue("@Thumbnail", thumbnail);
            command.Parameters.AddWithValue("@ModifiedAt", DateTime.UtcNow);

            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
            return rowsAffected > 0;
        }

        public async Task<string> ReadOcrResultAsync(long fileId, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand("SELECT OcrResult FROM Files WHERE Id = @Id AND DeletedAt IS NULL", connection);
            command.Parameters.AddWithValue("@Id", fileId);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var ocrResult = reader["OcrResult"] as string;
                return ocrResult ?? string.Empty;
            }

            return string.Empty;
        }

        public async Task<bool> WriteOcrResultAsync(long fileId, string ocrResult, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(@"
                UPDATE Files 
                SET OcrResult = @OcrResult, ModifiedAt = @ModifiedAt 
                WHERE Id = @Id AND DeletedAt IS NULL", connection);
            command.Parameters.AddWithValue("@Id", fileId);
            command.Parameters.AddWithValue("@OcrResult", ocrResult);
            command.Parameters.AddWithValue("@ModifiedAt", DateTime.UtcNow);

            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
            return rowsAffected > 0;
        }

        private static string GenerateFilePath(string fileName)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var randomSuffix = Guid.NewGuid().ToString("N")[..8];
            var extension = Path.GetExtension(fileName);
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            
            return $"files/{DateTime.UtcNow.Year}/{DateTime.UtcNow.Month:D2}/{timestamp}_{nameWithoutExt}_{randomSuffix}{extension}";
        }
    }
}
