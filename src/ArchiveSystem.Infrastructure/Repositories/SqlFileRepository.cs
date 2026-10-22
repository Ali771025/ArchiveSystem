// src/ArchiveSystem.Infrastructure/Repositories/SqlFileRepository.cs (Updated with Audit)
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ArchiveSystem.Application.Interfaces;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Repositories;
using FileEntity = ArchiveSystem.Domain.Entities.ArchiveFile;

namespace ArchiveSystem.Infrastructure.Repositories
{
    public class SqlFileRepository : IFileRepository
    {
        private readonly ArchiveDbContext _context;
        private readonly string _connectionString;
        private readonly IAuditService _auditService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<SqlFileRepository> _logger;

        public SqlFileRepository(
            ArchiveDbContext context, 
            IConfiguration configuration,
            IAuditService auditService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<SqlFileRepository> logger)
        {
            _context = context;
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            _auditService = auditService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<FileEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.Files
                .AsNoTracking()
                .Include(f => f.Case)
                .FirstOrDefaultAsync(f => f.Id == id && f.DeletedAt == null, cancellationToken);
        }

        public async Task<IReadOnlyList<FileEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Files
                .AsNoTracking()
                .Include(f => f.Case)
                .Where(f => f.DeletedAt == null)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<FileEntity>> GetByCaseIdAsync(long caseId, CancellationToken cancellationToken = default)
        {
            return await _context.Files
                .AsNoTracking()
                .Include(f => f.Case)
                .Where(f => f.CaseId == caseId && f.DeletedAt == null)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<FileEntity>> GetByStatusAsync(FileStatus status, CancellationToken cancellationToken = default)
        {
            return await _context.Files
                .AsNoTracking()
                .Include(f => f.Case)
                .Where(f => f.Status == status && f.DeletedAt == null)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<FileEntity>> GetByExtensionAsync(string extension, CancellationToken cancellationToken = default)
        {
            var fileExtension = Domain.ValueObjects.FileExtension.Create(extension);
            return await _context.Files
                .AsNoTracking()
                .Include(f => f.Case)
                .Where(f => f.Extension == fileExtension && f.DeletedAt == null)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<FileEntity>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            return await _context.Files
                .AsNoTracking()
                .Include(f => f.Case)
                .Where(f => f.DeletedAt == null)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<long> CountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Files
                .AsNoTracking()
                .Where(f => f.DeletedAt == null)
                .LongCountAsync(cancellationToken);
        }

        public async Task<long> CountByCaseIdAsync(long caseId, CancellationToken cancellationToken = default)
        {
            return await _context.Files
                .AsNoTracking()
                .Where(f => f.CaseId == caseId && f.DeletedAt == null)
                .LongCountAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.Files
                .AsNoTracking()
                .AnyAsync(f => f.Id == id && f.DeletedAt == null, cancellationToken);
        }

        public async Task AddAsync(FileEntity FileEntity, CancellationToken cancellationToken = default)
        {
            await _context.Files.AddAsync(FileEntity, cancellationToken);
        }

        public async Task UpdateAsync(FileEntity FileEntity, CancellationToken cancellationToken = default)
        {
            var oldFile = await GetByIdAsync(FileEntity.Id, cancellationToken);
            var oldFileSnapshot = oldFile != null ? SerializeFile(oldFile) : null;
            var newFileSnapshot = SerializeFile(FileEntity);

            _context.Files.Update(FileEntity);

            // Log the update operation
            var userId = GetUserId();
            var userName = GetUserName();
            var ipAddress = GetIpAddress();
            var userAgent = GetUserAgent();

            if (!string.IsNullOrEmpty(oldFileSnapshot))
            {
                await _auditService.LogOperationAsync(
                    userId, 
                    userName, 
                    "UPDATE", 
                    "Files", 
                    FileEntity.Id.ToString(), 
                    oldFileSnapshot, 
                    newFileSnapshot, 
                    ipAddress, 
                    userAgent
                );
            }
        }

        public async Task DeleteAsync(FileEntity FileEntity, CancellationToken cancellationToken = default)
        {
            // Soft delete - update status and set deletedAt
            var oldFile = await GetByIdAsync(FileEntity.Id, cancellationToken);
            var oldFileSnapshot = oldFile != null ? SerializeFile(oldFile) : null;

            FileEntity.SoftDelete(); // This sets Status to Deleted and DeletedAt to current time
            _context.Files.Update(FileEntity);

            // Log the delete operation
            var userId = GetUserId();
            var userName = GetUserName();
            var ipAddress = GetIpAddress();
            var userAgent = GetUserAgent();

            if (!string.IsNullOrEmpty(oldFileSnapshot))
            {
                await _auditService.LogOperationAsync(
                    userId, 
                    userName, 
                    "SOFT_DELETE", 
                    "Files", 
                    FileEntity.Id.ToString(), 
                    oldFileSnapshot, 
                    SerializeFile(FileEntity), 
                    ipAddress, 
                    userAgent
                );
            }
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        // New method for full-text search
        public async Task<(IReadOnlyList<FileEntity> Files, int TotalCount)> SearchByContentAsync(
            string searchTerm, 
            int pageIndex = 1, 
            int pageSize = 20, 
            CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand("[dbo].[sp_SearchFilesByContent]", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@SearchTerm", searchTerm);
            command.Parameters.AddWithValue("@PageIndex", pageIndex);
            command.Parameters.AddWithValue("@PageSize", pageSize);

            var totalRecordsParam = new SqlParameter("@TotalRecords", System.Data.SqlDbType.Int) { Direction = System.Data.ParameterDirection.Output };
            command.Parameters.Add(totalRecordsParam);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var files = new List<FileEntity>();
            var idOrdinal = reader.GetOrdinal("Id");
            var nameOrdinal = reader.GetOrdinal("Name");
            var sizeOrdinal = reader.GetOrdinal("Size");
            var extensionOrdinal = reader.GetOrdinal("Extension");
            var contentTypeOrdinal = reader.GetOrdinal("ContentType");
            var storagePathOrdinal = reader.GetOrdinal("StoragePath");
            var statusOrdinal = reader.GetOrdinal("Status");
            var caseIdOrdinal = reader.GetOrdinal("CaseId");
            while (await reader.ReadAsync(cancellationToken))
            {
                var FileEntity = new FileEntity(
                    id: reader.GetInt64(idOrdinal),
                    name: reader.GetString(nameOrdinal),
                    size: Domain.ValueObjects.FileSize.Create(reader.GetInt64(sizeOrdinal)),
                    extension: Domain.ValueObjects.FileExtension.Create(reader.GetString(extensionOrdinal)),
                    contentType: reader.GetString(contentTypeOrdinal),
                    storagePath: reader.GetString(storagePathOrdinal),
                    caseId: reader.IsDBNull(caseIdOrdinal) ? null : reader.GetInt64(caseIdOrdinal)
                );

                FileEntity.UpdateStatus((FileStatus)reader.GetByte(statusOrdinal));
                FileEntity.MarkAsModified(); // This will set ModifiedAt if needed

                files.Add(FileEntity);
            }

            var totalCount = (int)totalRecordsParam.Value;
            return (files.AsReadOnly(), totalCount);
        }

        private string SerializeFile(FileEntity FileEntity)
        {
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                Id = FileEntity.Id,
                Name = FileEntity.Name,
                Size = FileEntity.Size.Bytes,
                Extension = FileEntity.Extension.Value,
                ContentType = FileEntity.ContentType,
                StoragePath = FileEntity.StoragePath,
                Status = FileEntity.Status,
                CaseId = FileEntity.CaseId,
                ClassificationId = FileEntity.Case?.ClassificationId,
                CreatedAt = FileEntity.CreatedAt,
                ModifiedAt = FileEntity.ModifiedAt,
                DeletedAt = FileEntity.DeletedAt,
                Tags = FileEntity.Tags
            });
        }

        private long GetUserId()
        {
            var userIdClaim = _httpContextAccessor?.HttpContext?.User?.FindFirst("sub")?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private string GetUserName()
        {
            return _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "System";
        }

        private string GetIpAddress()
        {
            return _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private string GetUserAgent()
        {
            return _httpContextAccessor?.HttpContext?.Request?.Headers["User-Agent"].ToString() ?? "Unknown";
        }
    }
}
