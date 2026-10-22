using ArchiveSystem.Application.AccessControl;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Infrastructure.Repositories
{
    public class EfFileTagRepository : IFileTagRepository
    {
        private readonly ArchiveDbContext _dbContext;

        public EfFileTagRepository(ArchiveDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyCollection<long>> GetTagIdsByFileIdAsync(long fileId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.FileTags
                .AsNoTracking()
                .Where(x => x.FileId == fileId)
                .Select(x => x.TagId)
                .ToArrayAsync(cancellationToken);
        }

        public async Task SetFileTagsAsync(long fileId, IReadOnlyCollection<long> tagIds, CancellationToken cancellationToken = default)
        {
            var existing = _dbContext.FileTags.Where(x => x.FileId == fileId);
            _dbContext.FileTags.RemoveRange(existing);

            foreach (var tagId in tagIds.Distinct())
            {
                _dbContext.FileTags.Add(new FileTag
                {
                    FileId = fileId,
                    TagId = tagId
                });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
