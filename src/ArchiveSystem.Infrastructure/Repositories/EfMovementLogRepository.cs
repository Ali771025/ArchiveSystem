using ArchiveSystem.Application.Common.Models;
using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.Reports.Models;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Infrastructure.Repositories
{
    public class EfMovementLogRepository : IMovementLogRepository
    {
        private readonly ArchiveDbContext _dbContext;

        public EfMovementLogRepository(ArchiveDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(MovementLog movementLog, CancellationToken cancellationToken = default)
        {
            await _dbContext.MovementLogs.AddAsync(movementLog, cancellationToken);
        }

        public async Task<PaginatedResult<ActivityReportItemDto>> QueryActivityAsync(
            ActivityReportFilter filter,
            ActivityReportContext context,
            CancellationToken cancellationToken = default)
        {
            var normalizedPage = filter.Page < 1 ? 1 : filter.Page;
            var normalizedPageSize = filter.PageSize < 1 ? 50 : Math.Min(filter.PageSize, 500);

            var query = BuildFilteredAuthorizedQuery(filter, context);

            query = ApplySorting(query, filter);

            var totalCount = await query.CountAsync(cancellationToken);
            var skip = (normalizedPage - 1) * normalizedPageSize;

            var items = await query
                .Skip(skip)
                .Take(normalizedPageSize)
                .Select(MapToDtoExpression())
                .ToListAsync(cancellationToken);

            return new PaginatedResult<ActivityReportItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = normalizedPage,
                PageSize = normalizedPageSize
            };
        }

        public async Task<ReportSummaryDto> QuerySummaryAsync(
            ActivityReportFilter filter,
            ActivityReportContext context,
            CancellationToken cancellationToken = default)
        {
            var movementQuery = BuildFilteredAuthorizedQuery(filter, context);
            var actionCounts = await movementQuery
                .GroupBy(x => x.ActionType)
                .Select(g => new ActionCountDto
                {
                    ActionType = g.Key,
                    Count = g.LongCount()
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync(cancellationToken);

            var fileQuery =
                from file in _dbContext.Files.AsNoTracking()
                join @case in _dbContext.Cases.AsNoTracking() on file.CaseId equals @case.Id
                where file.DeletedAt == null && @case.DeletedAt == null
                select new SummaryFileProjection
                {
                    FileId = file.Id,
                    ClassificationId = @case.ClassificationId,
                    CreatedAt = file.CreatedAt,
                    SizeBytes = EF.Property<long>(file, "Size")
                };

            fileQuery = ApplySummaryFileFilters(fileQuery, filter, context);

            var classificationCounts = await fileQuery
                .GroupBy(x => x.ClassificationId)
                .Select(g => new ClassificationCountDto
                {
                    ClassificationId = g.Key,
                    Count = g.LongCount()
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync(cancellationToken);

            var totalFiles = await fileQuery.LongCountAsync(cancellationToken);
            var totalStorage = await fileQuery.SumAsync(x => (long?)x.SizeBytes, cancellationToken) ?? 0L;

            return new ReportSummaryDto
            {
                CountsByClassification = classificationCounts,
                CountsByActionType = actionCounts,
                TotalFiles = totalFiles,
                TotalStorageBytes = totalStorage
            };
        }

        public async IAsyncEnumerable<ActivityReportItemDto> StreamActivityAsync(
            ActivityReportFilter filter,
            ActivityReportContext context,
            int batchSize,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var take = batchSize <= 0 ? 1000 : Math.Min(batchSize, 5000);
            DateTime? lastTimestamp = null;
            long? lastId = null;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var pageQuery = BuildFilteredAuthorizedQuery(filter, context);
                pageQuery = pageQuery.OrderByDescending(x => x.Timestamp).ThenByDescending(x => x.Id);

                if (lastTimestamp.HasValue && lastId.HasValue)
                {
                    var timestamp = lastTimestamp.Value;
                    var id = lastId.Value;
                    pageQuery = pageQuery.Where(x => x.Timestamp < timestamp || (x.Timestamp == timestamp && x.Id < id));
                }

                var batch = await pageQuery
                    .Take(take)
                    .Select(MapToDtoExpression())
                    .ToListAsync(cancellationToken);

                if (batch.Count == 0)
                {
                    yield break;
                }

                foreach (var item in batch)
                {
                    yield return item;
                }

                var tail = batch[^1];
                lastTimestamp = tail.Timestamp;
                lastId = tail.Id;
            }
        }

        private IQueryable<MovementLog> BuildFilteredAuthorizedQuery(ActivityReportFilter filter, ActivityReportContext context)
        {
            var query = _dbContext.MovementLogs.AsNoTracking().AsQueryable();
            query = ApplyFilter(query, filter);
            query = ApplyAuthorization(query, context);
            return query;
        }

        private IQueryable<MovementLog> ApplyFilter(IQueryable<MovementLog> query, ActivityReportFilter filter)
        {
            if (filter.FromUtc.HasValue)
            {
                query = query.Where(x => x.Timestamp >= filter.FromUtc.Value);
            }

            if (filter.ToUtc.HasValue)
            {
                query = query.Where(x => x.Timestamp <= filter.ToUtc.Value);
            }

            if (filter.ActorUserId.HasValue)
            {
                query = query.Where(x => x.ActorUserId == filter.ActorUserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.ActionType))
            {
                query = query.Where(x => x.ActionType == filter.ActionType);
            }

            if (filter.ClassificationId.HasValue)
            {
                query = query.Where(x => x.ClassificationId == filter.ClassificationId.Value);
            }

            if (filter.CaseId.HasValue)
            {
                query = query.Where(x => x.CaseId == filter.CaseId.Value);
            }

            if (filter.FileId.HasValue)
            {
                query = query.Where(x => x.FileId == filter.FileId.Value);
            }

            if (filter.TagId.HasValue)
            {
                query = query.Where(x => x.TagId == filter.TagId.Value);
            }

            return query;
        }

        private IQueryable<MovementLog> ApplySorting(IQueryable<MovementLog> query, ActivityReportFilter filter)
        {
            var descending = filter.SortDescending;
            return (filter.SortBy ?? string.Empty).ToLowerInvariant() switch
            {
                "actor" => descending ? query.OrderByDescending(x => x.ActorUserId).ThenByDescending(x => x.Timestamp) : query.OrderBy(x => x.ActorUserId).ThenBy(x => x.Timestamp),
                "actiontype" => descending ? query.OrderByDescending(x => x.ActionType).ThenByDescending(x => x.Timestamp) : query.OrderBy(x => x.ActionType).ThenBy(x => x.Timestamp),
                "entitytype" => descending ? query.OrderByDescending(x => x.EntityType).ThenByDescending(x => x.Timestamp) : query.OrderBy(x => x.EntityType).ThenBy(x => x.Timestamp),
                _ => descending ? query.OrderByDescending(x => x.Timestamp).ThenByDescending(x => x.Id) : query.OrderBy(x => x.Timestamp).ThenBy(x => x.Id)
            };
        }

        private IQueryable<MovementLog> ApplyAuthorization(IQueryable<MovementLog> query, ActivityReportContext context)
        {
            if (context.IsAdmin)
            {
                return query;
            }

            var allowedClassifications = context.AllowedClassificationIds.Distinct().ToArray();
            var allowedTags = context.AllowedTagIds.Distinct().ToArray();
            if (allowedClassifications.Length == 0 && allowedTags.Length == 0)
            {
                return query.Where(_ => false);
            }

            query = query.Where(x =>
                (x.ClassificationId.HasValue && allowedClassifications.Contains(x.ClassificationId.Value)) ||
                (!x.ClassificationId.HasValue && x.CaseId.HasValue &&
                    _dbContext.Cases.Any(c => c.Id == x.CaseId.Value && c.DeletedAt == null && allowedClassifications.Contains(c.ClassificationId))) ||
                (!x.ClassificationId.HasValue && !x.CaseId.HasValue && x.FileId.HasValue &&
                    _dbContext.Files
                        .Where(f => f.Id == x.FileId.Value && f.DeletedAt == null && f.CaseId.HasValue)
                        .Join(
                            _dbContext.Cases.Where(c => c.DeletedAt == null),
                            f => f.CaseId!.Value,
                            c => c.Id,
                            (_, c) => c.ClassificationId)
                        .Any(classificationId => allowedClassifications.Contains(classificationId))) ||
                (!x.ClassificationId.HasValue && x.TagId.HasValue && allowedTags.Contains(x.TagId.Value)));

            query = query.Where(x => !x.TagId.HasValue || allowedTags.Contains(x.TagId.Value));

            // For file-related events, enforce full tag-visibility semantics.
            query = query.Where(x =>
                !x.FileId.HasValue ||
                !_dbContext.FileTags.Any(ft => ft.FileId == x.FileId.Value && !allowedTags.Contains(ft.TagId)));

            return query;
        }

        private IQueryable<SummaryFileProjection> ApplySummaryFileFilters(
            IQueryable<SummaryFileProjection> query,
            ActivityReportFilter filter,
            ActivityReportContext context)
        {
            if (!context.IsAdmin)
            {
                var allowedClassifications = context.AllowedClassificationIds.Distinct().ToArray();
                var allowedTags = context.AllowedTagIds.Distinct().ToArray();

                if (allowedClassifications.Length == 0)
                {
                    return query.Where(_ => false);
                }

                query = query.Where(x => allowedClassifications.Contains(x.ClassificationId));
                query = query.Where(x =>
                    !_dbContext.FileTags.Any(ft => ft.FileId == x.FileId && !allowedTags.Contains(ft.TagId)));
            }

            if (filter.FromUtc.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= filter.FromUtc.Value);
            }

            if (filter.ToUtc.HasValue)
            {
                query = query.Where(x => x.CreatedAt <= filter.ToUtc.Value);
            }

            if (filter.ClassificationId.HasValue)
            {
                query = query.Where(x => x.ClassificationId == filter.ClassificationId.Value);
            }

            if (filter.CaseId.HasValue)
            {
                query = query.Where(x => _dbContext.Files.Any(f => f.Id == x.FileId && f.CaseId == filter.CaseId.Value));
            }

            if (filter.FileId.HasValue)
            {
                query = query.Where(x => x.FileId == filter.FileId.Value);
            }

            if (filter.TagId.HasValue)
            {
                query = query.Where(x => _dbContext.FileTags.Any(ft => ft.FileId == x.FileId && ft.TagId == filter.TagId.Value));
            }

            return query;
        }

        private static System.Linq.Expressions.Expression<Func<MovementLog, ActivityReportItemDto>> MapToDtoExpression()
        {
            return x => new ActivityReportItemDto
            {
                Id = x.Id,
                Timestamp = x.Timestamp,
                ActorUserId = x.ActorUserId,
                ActorName = x.ActorName,
                ActionType = x.ActionType,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                ClassificationId = x.ClassificationId,
                CaseId = x.CaseId,
                FileId = x.FileId,
                FileVersionId = x.FileVersionId,
                TagId = x.TagId,
                Ip = x.Ip,
                UserAgent = x.UserAgent,
                BeforeJson = x.BeforeJson,
                AfterJson = x.AfterJson
            };
        }

        private sealed class SummaryFileProjection
        {
            public long FileId { get; set; }
            public long ClassificationId { get; set; }
            public DateTime CreatedAt { get; set; }
            public long SizeBytes { get; set; }
        }
    }
}
