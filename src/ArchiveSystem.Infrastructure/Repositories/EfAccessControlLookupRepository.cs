using ArchiveSystem.Application.AccessControl;
using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Infrastructure.Repositories
{
    public class EfAccessControlLookupRepository : IAccessControlLookupRepository
    {
        private readonly ArchiveDbContext _dbContext;

        public EfAccessControlLookupRepository(ArchiveDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<ClassificationNode>> GetClassificationNodesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.Classifications
                .AsNoTracking()
                .Select(x => new ClassificationNode(x.Id, x.ParentId))
                .ToListAsync(cancellationToken);
        }

        public async Task<long?> GetClassificationIdByCaseIdAsync(long caseId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Cases
                .AsNoTracking()
                .Where(x => x.Id == caseId && x.DeletedAt == null)
                .Select(x => (long?)x.ClassificationId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<FileAccessDescriptor?> GetFileAccessDescriptorAsync(long fileId, CancellationToken cancellationToken = default)
        {
            var fileInfo = await _dbContext.Files
                .AsNoTracking()
                .Where(x => x.Id == fileId && x.DeletedAt == null)
                .Select(x => new
                {
                    x.Id,
                    x.CaseId
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (fileInfo == null)
            {
                return null;
            }

            long? classificationId = null;
            if (fileInfo.CaseId.HasValue)
            {
                classificationId = await _dbContext.Cases
                    .AsNoTracking()
                    .Where(x => x.Id == fileInfo.CaseId.Value && x.DeletedAt == null)
                    .Select(x => (long?)x.ClassificationId)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            var tagIds = await _dbContext.FileTags
                .AsNoTracking()
                .Where(x => x.FileId == fileId)
                .Select(x => x.TagId)
                .ToArrayAsync(cancellationToken);

            return new FileAccessDescriptor(fileId, classificationId, tagIds);
        }
    }
}
