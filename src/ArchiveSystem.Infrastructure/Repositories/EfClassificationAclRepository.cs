using AccessControlRepositories = ArchiveSystem.Application.AccessControl;
using CommonRepositories = ArchiveSystem.Application.Common.Interfaces.Repositories;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Infrastructure.Repositories
{
    public class EfClassificationAclRepository : AccessControlRepositories.IClassificationAclRepository, CommonRepositories.IClassificationAclRepository
    {
        private readonly ArchiveDbContext _dbContext;

        public EfClassificationAclRepository(ArchiveDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<ClassificationAclEntry>> GetByClassificationIdAsync(long classificationId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.ClassificationAclEntries
                .AsNoTracking()
                .Where(x => x.ClassificationId == classificationId)
                .OrderBy(x => x.SubjectType)
                .ThenBy(x => x.SubjectId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ClassificationAclEntry>> GetByClassificationIdAndDescendantsAsync(
            long classificationId,
            CancellationToken cancellationToken = default)
        {
            var classificationIds = await GetClassificationAndDescendantIdsAsync(classificationId, cancellationToken);
            return await _dbContext.ClassificationAclEntries
                .AsNoTracking()
                .Where(x => classificationIds.Contains(x.ClassificationId))
                .OrderBy(x => x.ClassificationId)
                .ThenBy(x => x.SubjectType)
                .ThenBy(x => x.SubjectId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ClassificationAclEntry>> GetBySubjectsAsync(
            long userId,
            IReadOnlyCollection<long> roleIds,
            CancellationToken cancellationToken = default)
        {
            var roles = roleIds.Distinct().ToArray();
            return await _dbContext.ClassificationAclEntries
                .AsNoTracking()
                .Where(x =>
                    (x.SubjectType == AccessSubjectType.User && x.SubjectId == userId) ||
                    (x.SubjectType == AccessSubjectType.Role && roles.Contains(x.SubjectId)))
                .ToListAsync(cancellationToken);
        }

        public async Task GrantUserAccessAsync(
            long userId,
            long classificationId,
            ClassificationPermissionMask permissionMask,
            CancellationToken cancellationToken = default)
        {
            var existing = await _dbContext.ClassificationAclEntries
                .FirstOrDefaultAsync(
                    entry =>
                        entry.SubjectType == AccessSubjectType.User &&
                        entry.SubjectId == userId &&
                        entry.ClassificationId == classificationId,
                    cancellationToken);

            if (existing is null)
            {
                _dbContext.ClassificationAclEntries.Add(new ClassificationAclEntry(
                    id: 0,
                    subjectType: AccessSubjectType.User,
                    subjectId: userId,
                    classificationId: classificationId,
                    permissionMask: permissionMask,
                    isExplicit: true,
                    inheritMode: ClassificationAclInheritMode.Override));
            }
            else
            {
                existing.UpdateMask(
                    existing.PermissionMask | permissionMask,
                    ClassificationAclInheritMode.Override);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task ReplaceForClassificationAsync(
            long classificationId,
            IReadOnlyCollection<ClassificationAclEntry> entries,
            CancellationToken cancellationToken = default)
        {
            await ReplaceForClassificationsAsync(new[] { classificationId }, entries, cancellationToken);
        }

        public async Task ReplaceForClassificationAndDescendantsAsync(
            long classificationId,
            IReadOnlyCollection<ClassificationAclEntry> entries,
            CancellationToken cancellationToken = default)
        {
            var classificationIds = await GetClassificationAndDescendantIdsAsync(classificationId, cancellationToken);
            await ReplaceForClassificationsAsync(classificationIds, entries, cancellationToken);
        }

        public async Task OverrideForDescendantsAsync(
            long classificationId,
            IReadOnlyCollection<ClassificationAclEntry> entries,
            CancellationToken cancellationToken = default)
        {
            if (entries.Count == 0)
            {
                return;
            }

            var classificationIds = (await GetClassificationAndDescendantIdsAsync(
                    classificationId,
                    cancellationToken))
                .Where(id => id != classificationId)
                .ToArray();
            if (classificationIds.Length == 0)
            {
                return;
            }

            var subjectKeys = entries
                .Select(entry => (entry.SubjectType, entry.SubjectId))
                .ToHashSet();
            var existing = await _dbContext.ClassificationAclEntries
                .Where(entry => classificationIds.Contains(entry.ClassificationId))
                .ToListAsync(cancellationToken);

            _dbContext.ClassificationAclEntries.RemoveRange(
                existing.Where(entry => subjectKeys.Contains((entry.SubjectType, entry.SubjectId))));

            foreach (var targetClassificationId in classificationIds)
            {
                foreach (var entry in entries)
                {
                    _dbContext.ClassificationAclEntries.Add(new ClassificationAclEntry(
                        id: 0,
                        subjectType: entry.SubjectType,
                        subjectId: entry.SubjectId,
                        classificationId: targetClassificationId,
                        permissionMask: entry.PermissionMask,
                        isExplicit: true,
                        inheritMode: entry.InheritMode));
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task<IReadOnlyCollection<long>> GetClassificationAndDescendantIdsAsync(
            long classificationId,
            CancellationToken cancellationToken)
        {
            var root = await _dbContext.Classifications
                .AsNoTracking()
                .Where(x => x.Id == classificationId)
                .Select(x => new { x.Id, x.Path })
                .FirstOrDefaultAsync(cancellationToken);

            if (root is null)
            {
                return new[] { classificationId };
            }

            var descendantPrefix = root.Path.EndsWith($"/{root.Id}/", StringComparison.Ordinal)
                ? root.Path
                : $"{root.Path.TrimEnd('/')}/{root.Id}/";

            return await _dbContext.Classifications
                .AsNoTracking()
                .Where(x => x.Id == classificationId || x.Path.StartsWith(descendantPrefix))
                .Select(x => x.Id)
                .ToArrayAsync(cancellationToken);
        }

        private async Task ReplaceForClassificationsAsync(
            IReadOnlyCollection<long> classificationIds,
            IReadOnlyCollection<ClassificationAclEntry> entries,
            CancellationToken cancellationToken)
        {
            var targetIds = classificationIds.Where(id => id > 0).Distinct().ToArray();
            if (targetIds.Length == 0)
            {
                return;
            }

            var existing = _dbContext.ClassificationAclEntries.Where(x => targetIds.Contains(x.ClassificationId));
            _dbContext.ClassificationAclEntries.RemoveRange(existing);

            foreach (var targetClassificationId in targetIds)
            {
                foreach (var entry in entries)
                {
                    _dbContext.ClassificationAclEntries.Add(new ClassificationAclEntry(
                        id: 0,
                        subjectType: entry.SubjectType,
                        subjectId: entry.SubjectId,
                        classificationId: targetClassificationId,
                        permissionMask: entry.PermissionMask,
                        isExplicit: true,
                        inheritMode: entry.InheritMode));
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
