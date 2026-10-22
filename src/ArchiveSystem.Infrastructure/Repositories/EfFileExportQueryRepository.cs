using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.Reports.Models;
using ArchiveSystem.Application.Common;
using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Infrastructure.Repositories
{
    public class EfFileExportQueryRepository : IFileExportQueryRepository
    {
        private readonly ArchiveDbContext _dbContext;

        public EfFileExportQueryRepository(ArchiveDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async IAsyncEnumerable<FileExportItemDto> StreamFilesForZipExportAsync(
            FilesZipExportRequest request,
            ActivityReportContext context,
            int batchSize,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var take = batchSize <= 0 ? 200 : Math.Min(batchSize, 2000);
            long? lastFileId = null;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var query =
                    from file in _dbContext.Files.AsNoTracking()
                    join @case in _dbContext.Cases.AsNoTracking() on file.CaseId equals @case.Id
                    join version in _dbContext.FileVersions.AsNoTracking() on file.LatestVersionId equals version.Id
                    where file.DeletedAt == null
                          && @case.DeletedAt == null
                          && version.DeletedAt == null
                          && file.LatestVersionId.HasValue
                    select new FileRowProjection
                    {
                        Id = file.Id,
                        Name = file.Name,
                        CaseId = file.CaseId,
                        ClassificationId = @case.ClassificationId,
                        CaseNumber = @case.CaseNumber,
                        CaseTitle = @case.Title,
                        CaseOwners = @case.Owners,
                        CaseLocation = @case.Location,
                        CasePostalCode = @case.PostalCode,
                        VersionId = version.Id,
                        VersionNumber = version.VersionNumber,
                        FileName = version.FileName,
                        ContentType = version.ContentType,
                        SizeBytes = version.Size,
                        ObjectKey = version.StoragePath,
                        CreatedAt = file.CreatedAt
                    };

                query = ApplyRequestFilters(query, request);
                query = ApplyAuthorization(query, context);

                if (lastFileId.HasValue)
                {
                    query = query.Where(x => x.Id > lastFileId.Value);
                }

                var batch = await query
                    .OrderBy(x => x.Id)
                    .Take(take)
                    .ToListAsync(cancellationToken);

                if (batch.Count == 0)
                {
                    yield break;
                }

                var fileIds = batch.Select(x => x.Id).Distinct().ToArray();
                var tagRows = await _dbContext.FileTags
                    .AsNoTracking()
                    .Where(x => fileIds.Contains(x.FileId))
                    .Select(x => new { x.FileId, x.TagId })
                    .ToListAsync(cancellationToken);

                var tagMap = tagRows
                    .GroupBy(x => x.FileId)
                    .ToDictionary(g => g.Key, g => (IReadOnlyCollection<long>)g.Select(x => x.TagId).Distinct().ToArray());

                foreach (var item in batch)
                {
                    yield return new FileExportItemDto
                    {
                        FileId = item.Id,
                        CaseId = item.CaseId,
                        ClassificationId = item.ClassificationId,
                        CaseNumber = item.CaseNumber,
                        CaseTitle = item.CaseTitle,
                        CaseOwners = string.Join("، ", OwnerDisplayNameParser.ParseNames(item.CaseOwners)),
                        CaseLocation = item.CaseLocation,
                        CasePostalCode = item.CasePostalCode,
                        VersionId = item.VersionId,
                        VersionNumber = item.VersionNumber,
                        FileName = string.IsNullOrWhiteSpace(item.FileName) ? item.Name : item.FileName,
                        ContentType = item.ContentType,
                        SizeBytes = item.SizeBytes,
                        ObjectKey = item.ObjectKey,
                        TagIds = tagMap.TryGetValue(item.Id, out var tags) ? tags : Array.Empty<long>()
                    };
                }

                lastFileId = batch[^1].Id;
            }
        }

        private IQueryable<FileRowProjection> ApplyRequestFilters(IQueryable<FileRowProjection> query, FilesZipExportRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.Term))
            {
                var like = $"%{request.Term.Trim()}%";
                query = query.Where(x => EF.Functions.Like(x.Name, like));
            }

            if (request.FromUtc.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= request.FromUtc.Value);
            }

            if (request.ToUtc.HasValue)
            {
                query = query.Where(x => x.CreatedAt <= request.ToUtc.Value);
            }

            if (request.ClassificationId.HasValue)
            {
                query = query.Where(x => x.ClassificationId == request.ClassificationId.Value);
            }

            if (request.CaseId.HasValue)
            {
                query = query.Where(x => x.CaseId == request.CaseId.Value);
            }

            if (request.FileId.HasValue)
            {
                query = query.Where(x => x.Id == request.FileId.Value);
            }

            if (request.TagId.HasValue)
            {
                query = query.Where(x => _dbContext.FileTags.Any(ft => ft.FileId == x.Id && ft.TagId == request.TagId.Value));
            }

            return query;
        }

        private IQueryable<FileRowProjection> ApplyAuthorization(IQueryable<FileRowProjection> query, ActivityReportContext context)
        {
            if (context.IsAdmin)
            {
                return query;
            }

            var allowedClassifications = context.AllowedClassificationIds.Distinct().ToArray();
            var allowedTags = context.AllowedTagIds.Distinct().ToArray();
            if (allowedClassifications.Length == 0)
            {
                return query.Where(_ => false);
            }

            query = query.Where(x => allowedClassifications.Contains(x.ClassificationId));
            query = query.Where(x =>
                !_dbContext.FileTags.Any(ft => ft.FileId == x.Id && !allowedTags.Contains(ft.TagId)));

            return query;
        }

        private sealed class FileRowProjection
        {
            public long Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public long? CaseId { get; set; }
            public long ClassificationId { get; set; }
            public string CaseNumber { get; set; } = string.Empty;
            public string CaseTitle { get; set; } = string.Empty;
            public string CaseOwners { get; set; } = string.Empty;
            public string CaseLocation { get; set; } = string.Empty;
            public string? CasePostalCode { get; set; }
            public long VersionId { get; set; }
            public int VersionNumber { get; set; }
            public string FileName { get; set; } = string.Empty;
            public string ContentType { get; set; } = "application/octet-stream";
            public long SizeBytes { get; set; }
            public string ObjectKey { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }
    }
}
