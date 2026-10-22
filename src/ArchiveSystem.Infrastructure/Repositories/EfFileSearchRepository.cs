using ArchiveSystem.Application.Common.Models;
using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Infrastructure.Repositories
{
    public class EfFileSearchRepository : IFileSearchRepository
    {
        private readonly ArchiveDbContext _dbContext;

        public EfFileSearchRepository(ArchiveDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<PaginatedResult<FileDto>> SearchAsync(
            string? term,
            long? caseId,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize,
            string? sortBy,
            bool sortAscending,
            IReadOnlyCollection<long> allowedClassificationIds,
            bool bypassAuthorizationFilters,
            CancellationToken cancellationToken = default)
        {
            var normalizedPage = page < 1 ? 1 : page;
            var normalizedPageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 200);

            var allowedClassifications = allowedClassificationIds.Distinct().ToArray();
            if (!bypassAuthorizationFilters && allowedClassifications.Length == 0)
            {
                return new PaginatedResult<FileDto>
                {
                    Items = Array.Empty<FileDto>(),
                    TotalCount = 0,
                    Page = normalizedPage,
                    PageSize = normalizedPageSize
                };
            }

            var query =
                from file in _dbContext.Files.AsNoTracking()
                join @case in _dbContext.Cases.AsNoTracking() on file.CaseId equals @case.Id
                where file.DeletedAt == null && @case.DeletedAt == null
                select new
                {
                    File = file,
                    @case.ClassificationId
                };

            if (!bypassAuthorizationFilters)
            {
                query = query.Where(x => allowedClassifications.Contains(x.ClassificationId));
            }

            if (!string.IsNullOrWhiteSpace(term))
            {
                var like = $"%{term.Trim()}%";
                query = query.Where(x => EF.Functions.Like(x.File.Name, like));
            }

            if (fromDate.HasValue)
            {
                query = query.Where(x => x.File.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var inclusiveTo = toDate.Value.Date.AddDays(1);
                query = query.Where(x => x.File.CreatedAt < inclusiveTo);
            }

            if (caseId.HasValue)
            {
                query = query.Where(x => x.File.CaseId == caseId.Value);
            }

            query = (sortBy ?? string.Empty).ToLowerInvariant() switch
            {
                "name" => sortAscending ? query.OrderBy(x => x.File.Name) : query.OrderByDescending(x => x.File.Name),
                "size" => sortAscending
                    ? query.OrderBy(x => EF.Property<long>(x.File, "Size"))
                    : query.OrderByDescending(x => EF.Property<long>(x.File, "Size")),
                "status" => sortAscending ? query.OrderBy(x => x.File.Status) : query.OrderByDescending(x => x.File.Status),
                _ => sortAscending ? query.OrderBy(x => x.File.CreatedAt) : query.OrderByDescending(x => x.File.CreatedAt)
            };

            var totalCount = await query.CountAsync(cancellationToken);
            var skip = (normalizedPage - 1) * normalizedPageSize;

            var pageRows = await query
                .Skip(skip)
                .Take(normalizedPageSize)
                .ToListAsync(cancellationToken);

            var latestVersionIds = pageRows
                .Select(x => x.File.LatestVersionId)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .ToArray();

            var versionMap = latestVersionIds.Length == 0
                ? new Dictionary<long, Domain.Entities.FileVersion>()
                : await _dbContext.FileVersions
                    .AsNoTracking()
                    .Where(v => latestVersionIds.Contains(v.Id))
                    .ToDictionaryAsync(v => v.Id, cancellationToken);

            var pageFileIds = pageRows
                .Select(x => x.File.Id)
                .ToArray();

            var tagIdsByFileId = pageFileIds.Length == 0
                ? new Dictionary<long, long[]>()
                : (await _dbContext.FileTags
                    .AsNoTracking()
                    .Where(fileTag => pageFileIds.Contains(fileTag.FileId))
                    .Select(fileTag => new { fileTag.FileId, fileTag.TagId })
                    .ToListAsync(cancellationToken))
                    .GroupBy(fileTag => fileTag.FileId)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(fileTag => fileTag.TagId).ToArray());

            var items = pageRows
                .Select(row => new FileDto
                {
                    Id = row.File.Id,
                    Name = row.File.Name,
                    Size = row.File.Size.Bytes,
                    Extension = row.File.Extension.Value,
                    ContentType = row.File.ContentType,
                    Status = row.File.Status,
                    CaseId = row.File.CaseId,
                    LatestVersionNumber = row.File.LatestVersionId.HasValue && versionMap.TryGetValue(row.File.LatestVersionId.Value, out var version) ? version.VersionNumber : null,
                    ThumbnailStatus = row.File.LatestVersionId.HasValue && versionMap.TryGetValue(row.File.LatestVersionId.Value, out var versionForThumb) ? (int)versionForThumb.ThumbnailStatus : 0,
                    OcrStatus = row.File.LatestVersionId.HasValue && versionMap.TryGetValue(row.File.LatestVersionId.Value, out var versionForOcr) ? (int)versionForOcr.OcrStatus : 0,
                    OcrError = row.File.LatestVersionId.HasValue && versionMap.TryGetValue(row.File.LatestVersionId.Value, out var versionForOcrError) ? versionForOcrError.OcrError : null,
                    Tags = tagIdsByFileId.TryGetValue(row.File.Id, out var tagIds)
                        ? tagIds
                        : Array.Empty<long>(),
                    CreatedAt = row.File.CreatedAt,
                    ModifiedAt = row.File.ModifiedAt
                })
                .ToList();

            return new PaginatedResult<FileDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = normalizedPage,
                PageSize = normalizedPageSize
            };
        }
    }
}
