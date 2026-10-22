using AccessControlRepositories = ArchiveSystem.Application.AccessControl;
using CommonRepositories = ArchiveSystem.Application.Common.Interfaces.Repositories;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Infrastructure.Repositories
{
    public class EfTagAclRepository : AccessControlRepositories.ITagAclRepository, CommonRepositories.ITagAclRepository
    {
        private readonly ArchiveDbContext _dbContext;

        public EfTagAclRepository(ArchiveDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<TagAclEntry>> GetByTagIdAsync(long tagId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.TagAclEntries
                .AsNoTracking()
                .Where(x => x.TagId == tagId)
                .OrderBy(x => x.SubjectType)
                .ThenBy(x => x.SubjectId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<TagAclEntry>> GetBySubjectsAsync(
            long userId,
            IReadOnlyCollection<long> roleIds,
            CancellationToken cancellationToken = default)
        {
            var roles = roleIds.Distinct().ToArray();
            return await _dbContext.TagAclEntries
                .AsNoTracking()
                .Where(x =>
                    (x.SubjectType == AccessSubjectType.User && x.SubjectId == userId) ||
                    (x.SubjectType == AccessSubjectType.Role && roles.Contains(x.SubjectId)))
                .ToListAsync(cancellationToken);
        }

        public async Task ReplaceForTagAsync(long tagId, IReadOnlyCollection<TagAclEntry> entries, CancellationToken cancellationToken = default)
        {
            var existing = _dbContext.TagAclEntries.Where(x => x.TagId == tagId);
            _dbContext.TagAclEntries.RemoveRange(existing);

            foreach (var entry in entries)
            {
                _dbContext.TagAclEntries.Add(new TagAclEntry(
                    id: 0,
                    subjectType: entry.SubjectType,
                    subjectId: entry.SubjectId,
                    tagId: tagId,
                    permissionMask: entry.PermissionMask));
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
