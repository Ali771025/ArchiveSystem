using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Infrastructure.Repositories
{
    public class EfUploadSessionRepository : IUploadSessionRepository
    {
        private readonly ArchiveDbContext _dbContext;

        public EfUploadSessionRepository(ArchiveDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<UploadSession?> GetByIdAsync(Guid id, bool includeParts = false, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.UploadSessions.AsQueryable();
            if (includeParts)
            {
                query = query.Include(s => s.Parts);
            }

            return await query.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        public async Task AddAsync(UploadSession session, CancellationToken cancellationToken = default)
        {
            await _dbContext.UploadSessions.AddAsync(session, cancellationToken);
        }

        public Task UpdateAsync(UploadSession session, CancellationToken cancellationToken = default)
        {
            _dbContext.UploadSessions.Update(session);
            return Task.CompletedTask;
        }

        public async Task UpsertPartAsync(UploadPart part, CancellationToken cancellationToken = default)
        {
            var existing = await _dbContext.UploadParts
                .FirstOrDefaultAsync(
                    p => p.SessionId == part.SessionId && p.PartNumber == part.PartNumber,
                    cancellationToken);

            if (existing == null)
            {
                await _dbContext.UploadParts.AddAsync(part, cancellationToken);
                return;
            }

            existing.Replace(part.ETag, part.SizeBytes);
            _dbContext.UploadParts.Update(existing);
        }

        public async Task<IReadOnlyList<UploadPart>> ListPartsAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.UploadParts
                .Where(p => p.SessionId == sessionId)
                .OrderBy(p => p.PartNumber)
                .ToListAsync(cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
