using ArchiveSystem.Application.AccessControl;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ArchiveSystem.Domain.Common;

namespace ArchiveSystem.Infrastructure.Repositories
{
    public class EfTagRepository : ITagRepository
    {
        private readonly ArchiveDbContext _dbContext;

        public EfTagRepository(ArchiveDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.Tags
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);
        }

        public Task<Tag?> GetByIdAsync(long tagId, CancellationToken cancellationToken = default)
        {
            return _dbContext.Tags.FirstOrDefaultAsync(x => x.Id == tagId, cancellationToken);
        }

        public Task<Tag?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            var normalizedName = PersianTextNormalizer.NormalizeTagName(name);
            return _dbContext.Tags
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.NormalizedName == normalizedName, cancellationToken);
        }

        public Task AddAsync(Tag tag, CancellationToken cancellationToken = default)
        {
            return _dbContext.Tags.AddAsync(tag, cancellationToken).AsTask();
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
