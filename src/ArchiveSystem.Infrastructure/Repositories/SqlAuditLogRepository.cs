// src/ArchiveSystem.Infrastructure/Repositories/SqlAuditLogRepository.cs
using Microsoft.EntityFrameworkCore;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Repositories;
using ArchiveSystem.Infrastructure.Persistence;

namespace ArchiveSystem.Infrastructure.Repositories
{
    public class SqlAuditLogRepository : IAuditLogRepository
    {
        private readonly ArchiveDbContext _context;

        public SqlAuditLogRepository(ArchiveDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
        {
            await _context.AuditLogs.AddAsync(auditLog, cancellationToken);
        }

        public async Task<IReadOnlyList<AuditLog>> GetByUserIdAsync(long userId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var skip = (page - 1) * pageSize;
            return await _context.AuditLogs
                .AsNoTracking()
                .Where(al => al.UserId == userId)
                .OrderByDescending(al => al.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<AuditLog>> GetByFileIdAsync(long fileId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var skip = (page - 1) * pageSize;
            return await _context.AuditLogs
                .AsNoTracking()
                .Where(al => al.TableName == "Files" && al.RecordId == fileId.ToString())
                .OrderByDescending(al => al.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<AuditLog>> GetByActionAsync(string action, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var skip = (page - 1) * pageSize;
            return await _context.AuditLogs
                .AsNoTracking()
                .Where(al => al.Action == action)
                .OrderByDescending(al => al.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
