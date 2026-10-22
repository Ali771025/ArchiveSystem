// src/ArchiveSystem.Infrastructure/Repositories/SqlCaseRepository.cs (Updated with Audit)
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ArchiveSystem.Application.Interfaces;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Repositories;

namespace ArchiveSystem.Infrastructure.Repositories
{
    public class SqlCaseRepository : ICaseRepository
    {
        private readonly ArchiveDbContext _context;
        private readonly IAuditService _auditService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<SqlCaseRepository> _logger;

        public SqlCaseRepository(
            ArchiveDbContext context,
            IAuditService auditService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<SqlCaseRepository> logger)
        {
            _context = context;
            _auditService = auditService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<Case?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.Cases
                .AsNoTracking()
                .Include(c => c.Classification)
                .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null, cancellationToken);
        }

        public async Task<IReadOnlyList<Case>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Cases
                .AsNoTracking()
                .Include(c => c.Classification)
                .Where(c => c.DeletedAt == null)
                .ToListAsync(cancellationToken);
        }

        public async Task<Case?> GetByCaseNumberAsync(string caseNumber, CancellationToken cancellationToken = default)
        {
            return await _context.Cases
                .AsNoTracking()
                .Include(c => c.Classification)
                .FirstOrDefaultAsync(c => c.CaseNumber == caseNumber && c.DeletedAt == null, cancellationToken);
        }

        public async Task<Case?> GetByExternalCaseNumberAsync(string externalCaseNumber, CancellationToken cancellationToken = default)
        {
            return await _context.Cases
                .AsNoTracking()
                .Include(c => c.Classification)
                .FirstOrDefaultAsync(
                    c => c.ExternalCaseNumber == externalCaseNumber && c.DeletedAt == null,
                    cancellationToken);
        }

        public async Task<Case?> GetByCaseNumberFlexibleAsync(string requestKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(requestKey))
            {
                return null;
            }

            var normalized = requestKey.Trim();
            var exact = await GetByCaseNumberAsync(normalized, cancellationToken);
            if (exact != null)
            {
                return exact;
            }

            var alternate = normalized.StartsWith("A-", StringComparison.OrdinalIgnoreCase)
                ? normalized[2..]
                : $"A-{normalized}";
            if (!string.IsNullOrWhiteSpace(alternate))
            {
                var alternateExact = await GetByCaseNumberAsync(alternate, cancellationToken);
                if (alternateExact != null)
                {
                    return alternateExact;
                }
            }

            var normalizedRequest = NormalizeLookupKey(normalized);
            if (string.IsNullOrWhiteSpace(normalizedRequest))
            {
                return null;
            }

            var caseCandidates = await _context.Cases
                .AsNoTracking()
                .Include(c => c.Classification)
                .Where(c => c.DeletedAt == null)
                .ToListAsync(cancellationToken);

            return caseCandidates.FirstOrDefault(c =>
            {
                var normalizedCaseNumber = NormalizeLookupKey(c.CaseNumber);
                if (normalizedCaseNumber == normalizedRequest)
                {
                    return true;
                }

                if (normalizedCaseNumber.StartsWith('a') && normalizedCaseNumber.Length > 1)
                {
                    return normalizedCaseNumber[1..] == normalizedRequest;
                }

                return false;
            });
        }

        public async Task<IReadOnlyList<Case>> GetByClassificationIdAsync(long classificationId, CancellationToken cancellationToken = default)
        {
            return await _context.Cases
                .AsNoTracking()
                .Include(c => c.Classification)
                .Where(c => c.ClassificationId == classificationId && c.DeletedAt == null)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Case>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            return await _context.Cases
                .AsNoTracking()
                .Include(c => c.Classification)
                .Where(c => c.DeletedAt == null)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<long> CountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Cases.LongCountAsync(c => c.DeletedAt == null, cancellationToken);
        }

        public async Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.Cases.AnyAsync(c => c.Id == id && c.DeletedAt == null, cancellationToken);
        }

        public async Task AddAsync(Case caseEntity, CancellationToken cancellationToken = default)
        {
            await _context.Cases.AddAsync(caseEntity, cancellationToken);
            
            // Log the create operation
            var userId = GetUserId();
            var userName = GetUserName();
            var ipAddress = GetIpAddress();
            var userAgent = GetUserAgent();

            await _auditService.LogOperationAsync(
                userId, 
                userName, 
                "CREATE", 
                "Cases", 
                caseEntity.Id.ToString(), 
                null, 
                SerializeCase(caseEntity), 
                ipAddress, 
                userAgent
            );
        }

        public async Task<bool> MoveToClassificationAsync(
            long caseId,
            long classificationId,
            CancellationToken cancellationToken = default)
        {
            var modifiedAt = DateTime.UtcNow;
            var affectedRows = await _context.Cases
                .Where(c => c.Id == caseId && c.DeletedAt == null)
                .ExecuteUpdateAsync(
                    updates => updates
                        .SetProperty(c => c.ClassificationId, classificationId)
                        .SetProperty(c => c.ModifiedAt, modifiedAt),
                    cancellationToken);

            return affectedRows == 1;
        }

        public async Task UpdateAsync(Case caseEntity, CancellationToken cancellationToken = default)
        {
            var oldCase = await GetByIdAsync(caseEntity.Id, cancellationToken);
            var oldCaseSnapshot = oldCase != null ? SerializeCase(oldCase) : null;
            var newCaseSnapshot = SerializeCase(caseEntity);

            _context.Cases.Update(caseEntity);

            // Log the update operation
            var userId = GetUserId();
            var userName = GetUserName();
            var ipAddress = GetIpAddress();
            var userAgent = GetUserAgent();

            if (!string.IsNullOrEmpty(oldCaseSnapshot))
            {
                await _auditService.LogOperationAsync(
                    userId, 
                    userName, 
                    "UPDATE", 
                    "Cases", 
                    caseEntity.Id.ToString(), 
                    oldCaseSnapshot, 
                    newCaseSnapshot, 
                    ipAddress, 
                    userAgent
                );

                if (oldCase!.ClassificationId != caseEntity.ClassificationId)
                {
                    await _auditService.LogOperationAsync(
                        userId,
                        userName,
                        "CASE_MOVE_CLASSIFICATION",
                        "Cases",
                        caseEntity.Id.ToString(),
                        System.Text.Json.JsonSerializer.Serialize(new { oldClassificationId = oldCase.ClassificationId }),
                        System.Text.Json.JsonSerializer.Serialize(new { newClassificationId = caseEntity.ClassificationId }),
                        ipAddress,
                        userAgent);
                }
            }
        }

        public async Task DeleteAsync(Case caseEntity, CancellationToken cancellationToken = default)
        {
            // Soft delete - set DeletedAt
            var oldCase = await GetByIdAsync(caseEntity.Id, cancellationToken);
            var oldCaseSnapshot = oldCase != null ? SerializeCase(oldCase) : null;

            caseEntity.SoftDelete(); // This sets DeletedAt to current time
            _context.Cases.Update(caseEntity);

            // Log the delete operation
            var userId = GetUserId();
            var userName = GetUserName();
            var ipAddress = GetIpAddress();
            var userAgent = GetUserAgent();

            if (!string.IsNullOrEmpty(oldCaseSnapshot))
            {
                await _auditService.LogOperationAsync(
                    userId, 
                    userName, 
                    "SOFT_DELETE", 
                    "Cases", 
                    caseEntity.Id.ToString(), 
                    oldCaseSnapshot, 
                    SerializeCase(caseEntity), 
                    ipAddress, 
                    userAgent
                );
            }
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        private string SerializeCase(Case caseEntity)
        {
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                Id = caseEntity.Id,
                CaseNumber = caseEntity.CaseNumber,
                ExternalCaseNumber = caseEntity.ExternalCaseNumber,
                Title = caseEntity.Title,
                ClassificationId = caseEntity.ClassificationId,
                AvatarImagePath = caseEntity.AvatarImagePath,
                Owners = string.Join("، ", ArchiveSystem.Application.Common.OwnerDisplayNameParser.ParseNames(caseEntity.Owners)),
                Location = caseEntity.Location,
                ClosedAt = caseEntity.ClosedAt,
                CreatedAt = caseEntity.CreatedAt,
                ModifiedAt = caseEntity.ModifiedAt,
                DeletedAt = caseEntity.DeletedAt
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

        private static string NormalizeLookupKey(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var source = value.Trim().Normalize(NormalizationForm.FormKC);
            var buffer = new StringBuilder(source.Length);
            foreach (var rawChar in source)
            {
                var current = rawChar switch
                {
                    '٠' => '0',
                    '١' => '1',
                    '٢' => '2',
                    '٣' => '3',
                    '٤' => '4',
                    '٥' => '5',
                    '٦' => '6',
                    '٧' => '7',
                    '٨' => '8',
                    '٩' => '9',
                    '۰' => '0',
                    '۱' => '1',
                    '۲' => '2',
                    '۳' => '3',
                    '۴' => '4',
                    '۵' => '5',
                    '۶' => '6',
                    '۷' => '7',
                    '۸' => '8',
                    '۹' => '9',
                    _ => rawChar
                };

                if (char.IsWhiteSpace(current) ||
                    current == '-' ||
                    current == '_' ||
                    current == '/' ||
                    current == '\\' ||
                    current == '\u200c' ||
                    current == '\u200d' ||
                    current == '\u200e' ||
                    current == '\u200f')
                {
                    continue;
                }

                buffer.Append(char.ToLowerInvariant(current));
            }

            return buffer.ToString();
        }
    }
}
