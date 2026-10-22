using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Infrastructure.Repositories
{
    public class EfFileRepository : IFileRepository
    {
        private readonly ArchiveDbContext _dbContext;

        public EfFileRepository(ArchiveDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ArchiveFile?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Files
                .Include(f => f.Case)
                .Include(f => f.FileTags)
                .FirstOrDefaultAsync(f => f.Id == id && !f.DeletedAt.HasValue, cancellationToken);
        }

        public async Task<ArchiveFile?> FindActiveByCaseAndNameAsync(long caseId, string name, CancellationToken cancellationToken = default)
        {
            var normalizedName = name.Trim();
            return await _dbContext.Files
                .Include(f => f.Case)
                .Where(f => f.CaseId == caseId && !f.DeletedAt.HasValue)
                .OrderByDescending(f => f.ModifiedAt ?? f.CreatedAt)
                .FirstOrDefaultAsync(f => f.Name == normalizedName, cancellationToken);
        }

        public async Task AddAsync(ArchiveFile file, CancellationToken cancellationToken = default)
        {
            await _dbContext.Files.AddAsync(file, cancellationToken);
        }

        public Task UpdateAsync(ArchiveFile file, CancellationToken cancellationToken = default)
        {
            _dbContext.Files.Update(file);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
