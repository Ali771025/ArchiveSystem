using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.Reports.Models;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Infrastructure.Repositories;

public sealed class EfReportLogRepository : IReportLogRepository
{
    private readonly ArchiveDbContext _dbContext;

    public EfReportLogRepository(ArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AuditFilterOptionsDto> GetAuditFilterOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var entityTypes = new List<string>();
        var actionTypes = new List<string>();

        if (await TableExistsAsync("Logs_AuditTrail", cancellationToken))
        {
            entityTypes.AddRange(await _dbContext.AuditTrailLogs
                .AsNoTracking()
                .Select(log => log.TableName)
                .Distinct()
                .ToListAsync(cancellationToken));
            actionTypes.AddRange(await _dbContext.AuditTrailLogs
                .AsNoTracking()
                .Select(log => log.EventType)
                .Distinct()
                .ToListAsync(cancellationToken));
        }

        if (await TableExistsAsync("AuditLogs", cancellationToken))
        {
            entityTypes.AddRange(await _dbContext.AuditLogs
                .AsNoTracking()
                .Select(log => log.TableName)
                .Distinct()
                .ToListAsync(cancellationToken));
            actionTypes.AddRange(await _dbContext.AuditLogs
                .AsNoTracking()
                .Select(log => log.Action)
                .Distinct()
                .ToListAsync(cancellationToken));
        }

        return new AuditFilterOptionsDto
        {
            EntityTypes = entityTypes
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            ActionTypes = actionTypes
                .Select(NormalizeActionOption)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    public async Task<PagedResultDto<AuditLogResponseDto>> GetAuditTrailAsync(
        AuditFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(filter.PageNumber, 1);
        var pageSize = Math.Clamp(filter.PageSize, 1, 200);
        var takeCount = checked(pageNumber * pageSize);
        var userFilter = await ResolveAuditUserFilterAsync(filter.UserId, cancellationToken);

        var currentResult = await TableExistsAsync("Logs_AuditTrail", cancellationToken)
            ? await GetCurrentAuditTrailAsync(filter, userFilter, takeCount, cancellationToken)
            : (TotalCount: 0, Items: new List<AuditLogResponseDto>());

        var legacyResult = await TableExistsAsync("AuditLogs", cancellationToken)
            ? await GetLegacyAuditTrailAsync(filter, userFilter, takeCount, cancellationToken)
            : (TotalCount: 0, Items: new List<AuditLogResponseDto>());

        var items = currentResult.Items
            .Concat(legacyResult.Items)
            .OrderByDescending(x => x.ActionDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        await PopulateAuditUserNamesAsync(items, cancellationToken);

        return new PagedResultDto<AuditLogResponseDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = currentResult.TotalCount + legacyResult.TotalCount
        };
    }

    public async Task<PagedResultDto<SecurityLogResponseDto>> GetSecurityLogsAsync(
        SecurityLogFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(filter.PageNumber, 1);
        var pageSize = Math.Clamp(filter.PageSize, 1, 200);

        if (!await TableExistsAsync("Logs_Security", cancellationToken))
        {
            return new PagedResultDto<SecurityLogResponseDto>
            {
                Items = Array.Empty<SecurityLogResponseDto>(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = 0
            };
        }

        var query = _dbContext.SecurityLogs.AsNoTracking().AsQueryable();

        if (filter.StartDate.HasValue)
        {
            query = query.Where(x => x.ActionDate >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(x => x.ActionDate <= filter.EndDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.ActorUserId))
        {
            var actorUserId = filter.ActorUserId.Trim();
            query = query.Where(x => x.ActorUserId == actorUserId);
        }

        if (!string.IsNullOrWhiteSpace(filter.TargetUserId))
        {
            var targetUserId = filter.TargetUserId.Trim();
            query = query.Where(x => x.TargetUserId == targetUserId);
        }

        if (filter.EventTypes is { Count: > 0 })
        {
            var eventTypes = filter.EventTypes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToLower())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (eventTypes.Count > 0)
            {
                query = query.Where(x => x.EventType != null && eventTypes.Contains(x.EventType.ToLower()));
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await ApplySecurityLogSort(query, filter.SortBy, filter.SortDirection)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SecurityLogResponseDto
            {
                SecurityLogId = x.Id,
                EventType = x.EventType,
                ActorUserId = x.ActorUserId,
                TargetUserId = x.TargetUserId,
                Message = x.Message ?? string.Empty,
                ClientIpAddress = x.ClientIpAddress,
                Level = x.Level,
                ActionDate = x.ActionDate,
                SecurityMetadata = x.SecurityMetadata
            })
            .ToListAsync(cancellationToken);
        await PopulateSecurityUserNamesAsync(items, cancellationToken);

        return new PagedResultDto<SecurityLogResponseDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private static IOrderedQueryable<SecurityLog> ApplySecurityLogSort(
        IQueryable<SecurityLog> query,
        string? sortBy,
        string? sortDirection)
    {
        var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        return (sortBy ?? "ActionDate").Trim().ToLowerInvariant() switch
        {
            "eventtype" => descending
                ? query.OrderByDescending(x => x.EventType).ThenByDescending(x => x.ActionDate)
                : query.OrderBy(x => x.EventType).ThenByDescending(x => x.ActionDate),
            "actoruserid" => descending
                ? query.OrderByDescending(x => x.ActorUserId).ThenByDescending(x => x.ActionDate)
                : query.OrderBy(x => x.ActorUserId).ThenByDescending(x => x.ActionDate),
            "targetuserid" => descending
                ? query.OrderByDescending(x => x.TargetUserId).ThenByDescending(x => x.ActionDate)
                : query.OrderBy(x => x.TargetUserId).ThenByDescending(x => x.ActionDate),
            "level" => descending
                ? query.OrderByDescending(x => x.Level).ThenByDescending(x => x.ActionDate)
                : query.OrderBy(x => x.Level).ThenByDescending(x => x.ActionDate),
            "clientipaddress" => descending
                ? query.OrderByDescending(x => x.ClientIpAddress).ThenByDescending(x => x.ActionDate)
                : query.OrderBy(x => x.ClientIpAddress).ThenByDescending(x => x.ActionDate),
            _ => descending ? query.OrderByDescending(x => x.ActionDate) : query.OrderBy(x => x.ActionDate)
        };
    }

    private async Task<(int TotalCount, List<AuditLogResponseDto> Items)> GetCurrentAuditTrailAsync(
        AuditFilterDto filter,
        AuditUserFilter userFilter,
        int takeCount,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.AuditTrailLogs.AsNoTracking().AsQueryable();

        if (filter.StartDate.HasValue)
        {
            query = query.Where(x => x.ActionDate >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(x => x.ActionDate <= filter.EndDate.Value);
        }

        if (userFilter.TextValues.Count > 0)
        {
            query = query.Where(x => x.UserId != null && userFilter.TextValues.Contains(x.UserId));
        }

        var entities = NormalizeEntityTypes(filter.EntityTypes);
        if (entities.Count > 0)
        {
            query = query.Where(x => entities.Contains(x.TableName));
        }

        if (filter.ActionTypes is { Count: > 0 })
        {
            var actions = NormalizeRequestedActions(filter.ActionTypes);
            var includeCreate = actions.Contains("Create");
            var includeUpdate = actions.Contains("Update");
            var includeDelete = actions.Contains("Delete");
            var customActions = actions
                .Where(action => action is not ("Create" or "Update" or "Delete"))
                .ToList();

            query = query.Where(x =>
                (includeCreate && x.EventType.EndsWith("_Create")) ||
                (includeUpdate && x.EventType.EndsWith("_Update")) ||
                (includeDelete && x.EventType.EndsWith("_Delete")) ||
                customActions.Contains(x.EventType));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.ActionDate)
            .Take(takeCount)
            .Select(x => new AuditLogResponseDto
            {
                AuditId = x.AuditId,
                EventType = x.EventType,
                TableName = x.TableName,
                RecordId = x.RecordId,
                UserId = x.UserId,
                ActionDate = x.ActionDate,
                OldValues = x.OldValues,
                NewValues = x.NewValues,
                AffectedColumns = x.AffectedColumns
            })
            .ToListAsync(cancellationToken);

        return (totalCount, items);
    }

    private async Task<(int TotalCount, List<AuditLogResponseDto> Items)> GetLegacyAuditTrailAsync(
        AuditFilterDto filter,
        AuditUserFilter userFilter,
        int takeCount,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.AuditLogs.AsNoTracking().AsQueryable();

        if (filter.StartDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= filter.EndDate.Value);
        }

        if (userFilter.NumericValues.Count > 0)
        {
            query = query.Where(x => userFilter.NumericValues.Contains(x.UserId));
        }

        var entities = NormalizeEntityTypes(filter.EntityTypes);
        if (entities.Count > 0)
        {
            query = query.Where(x => entities.Contains(x.TableName));
        }

        if (filter.ActionTypes is { Count: > 0 })
        {
            var actions = NormalizeRequestedActions(filter.ActionTypes);
            var includeCreate = actions.Contains("Create");
            var includeUpdate = actions.Contains("Update");
            var includeDelete = actions.Contains("Delete");
            var customActions = actions
                .Where(action => action is not ("Create" or "Update" or "Delete"))
                .ToList();

            query = query.Where(x =>
                (includeCreate && EF.Functions.Like(x.Action, "%Create%")) ||
                (includeUpdate && EF.Functions.Like(x.Action, "%Update%")) ||
                (includeDelete && (EF.Functions.Like(x.Action, "%Delete%") || EF.Functions.Like(x.Action, "%Remove%"))) ||
                customActions.Contains(x.Action));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(takeCount)
            .Select(x => new AuditLogResponseDto
            {
                AuditId = -x.Id,
                EventType = x.Action,
                TableName = x.TableName,
                RecordId = x.RecordId,
                UserId = x.UserId.ToString(),
                UserName = x.UserName,
                ActionDate = x.CreatedAt,
                OldValues = x.OldValue,
                NewValues = x.NewValue,
                AffectedColumns = null
            })
            .ToListAsync(cancellationToken);

        return (totalCount, items);
    }

    private static List<string> NormalizeEntityTypes(IEnumerable<string>? requestedEntityTypes)
    {
        var entities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var requestedType in requestedEntityTypes ?? Array.Empty<string>())
        {
            var entityType = requestedType?.Trim();
            if (string.IsNullOrWhiteSpace(entityType))
            {
                continue;
            }

            if (entityType.Equals("Case", StringComparison.OrdinalIgnoreCase) ||
                entityType.Equals("Cases", StringComparison.OrdinalIgnoreCase) ||
                entityType.Equals("Dossier", StringComparison.OrdinalIgnoreCase) ||
                entityType.Equals("Dossiers", StringComparison.OrdinalIgnoreCase))
            {
                entities.Add("Cases");
                entities.Add("Dossiers");
                continue;
            }

            if (entityType.Equals("User", StringComparison.OrdinalIgnoreCase) ||
                entityType.Equals("Users", StringComparison.OrdinalIgnoreCase) ||
                entityType.Equals("AspNetUsers", StringComparison.OrdinalIgnoreCase))
            {
                entities.Add("Users");
                entities.Add("AspNetUsers");
                continue;
            }

            entities.Add(entityType);
        }

        return entities.ToList();
    }

    private static HashSet<string> NormalizeRequestedActions(IEnumerable<string> actions) =>
        actions
            .Select(NormalizeActionOption)
            .Where(action => !string.IsNullOrWhiteSpace(action))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static string NormalizeActionOption(string? action)
    {
        var normalized = action?.Trim() ?? string.Empty;
        var suffix = normalized.Split('_', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (suffix?.Equals("Create", StringComparison.OrdinalIgnoreCase) == true ||
            normalized.Equals("CREATE", StringComparison.OrdinalIgnoreCase))
        {
            return "Create";
        }

        if (suffix?.Equals("Update", StringComparison.OrdinalIgnoreCase) == true ||
            normalized.Equals("UPDATE", StringComparison.OrdinalIgnoreCase))
        {
            return "Update";
        }

        if (suffix?.Equals("Delete", StringComparison.OrdinalIgnoreCase) == true ||
            normalized.Equals("DELETE", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("SOFT_DELETE", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("REMOVE", StringComparison.OrdinalIgnoreCase))
        {
            return "Delete";
        }

        return normalized;
    }

    private async Task PopulateAuditUserNamesAsync(
        IReadOnlyCollection<AuditLogResponseDto> items,
        CancellationToken cancellationToken)
    {
        var userIds = items
            .Select(x => x.UserId)
            .Select(TryParseUserId)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();

        if (userIds.Count == 0)
        {
            return;
        }

        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(x => userIds.Contains(x.Id))
            .Select(x => new { x.Id, x.UserName })
            .ToDictionaryAsync(x => x.Id, x => x.UserName ?? string.Empty, cancellationToken);

        foreach (var item in items)
        {
            var userId = TryParseUserId(item.UserId);
            if (userId.HasValue && users.TryGetValue(userId.Value, out var userName) && !string.IsNullOrWhiteSpace(userName))
            {
                item.UserName = userName;
            }
        }
    }

    private async Task<AuditUserFilter> ResolveAuditUserFilterAsync(
        string? requestedUserId,
        CancellationToken cancellationToken)
    {
        var normalized = requestedUserId?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return AuditUserFilter.Empty;
        }

        var textValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { normalized };
        var numericValues = new HashSet<long>();
        var parsedUserId = TryParseUserId(normalized);
        if (parsedUserId.HasValue)
        {
            numericValues.Add(parsedUserId.Value);
        }

        var usersQuery = _dbContext.Users.AsNoTracking();
        var users = parsedUserId.HasValue
            ? await usersQuery
                .Where(x => x.Id == parsedUserId.Value)
                .Select(x => new { x.Id, x.UserName, x.NormalizedUserName })
                .ToListAsync(cancellationToken)
            : await usersQuery
                .Where(x => x.UserName == normalized || x.NormalizedUserName == normalized.ToUpper())
                .Select(x => new { x.Id, x.UserName, x.NormalizedUserName })
                .ToListAsync(cancellationToken);

        foreach (var user in users)
        {
            numericValues.Add(user.Id);
            textValues.Add(user.Id.ToString());

            if (!string.IsNullOrWhiteSpace(user.UserName))
            {
                textValues.Add(user.UserName);
            }

            if (!string.IsNullOrWhiteSpace(user.NormalizedUserName))
            {
                textValues.Add(user.NormalizedUserName);
            }
        }

        return new AuditUserFilter(textValues.ToList(), numericValues.ToList());
    }

    private async Task PopulateSecurityUserNamesAsync(
        IReadOnlyCollection<SecurityLogResponseDto> items,
        CancellationToken cancellationToken)
    {
        var userIds = items
            .SelectMany(x => new[] { x.ActorUserId, x.TargetUserId })
            .Select(TryParseUserId)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();

        if (userIds.Count == 0)
        {
            return;
        }

        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(x => userIds.Contains(x.Id))
            .Select(x => new { x.Id, x.UserName })
            .ToDictionaryAsync(x => x.Id, x => x.UserName ?? string.Empty, cancellationToken);

        foreach (var item in items)
        {
            var actorUserId = TryParseUserId(item.ActorUserId);
            if (actorUserId.HasValue && users.TryGetValue(actorUserId.Value, out var actorUserName) && !string.IsNullOrWhiteSpace(actorUserName))
            {
                item.ActorUserName = actorUserName;
            }

            var targetUserId = TryParseUserId(item.TargetUserId);
            if (targetUserId.HasValue && users.TryGetValue(targetUserId.Value, out var targetUserName) && !string.IsNullOrWhiteSpace(targetUserName))
            {
                item.TargetUserName = targetUserName;
            }
        }
    }

    private static long? TryParseUserId(string? userId)
    {
        return long.TryParse(userId?.Trim(), out var parsed) && parsed > 0 ? parsed : null;
    }

    private sealed record AuditUserFilter(
        IReadOnlyCollection<string> TextValues,
        IReadOnlyCollection<long> NumericValues)
    {
        public static readonly AuditUserFilter Empty = new(Array.Empty<string>(), Array.Empty<long>());
    }

    private async Task<bool> TableExistsAsync(string tableName, CancellationToken cancellationToken)
    {
        var result = await _dbContext.Database
            .SqlQuery<int>(
                $"""
                 SELECT CAST(
                     CASE
                         WHEN EXISTS (
                             SELECT 1
                             FROM sys.tables AS t
                             INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
                             WHERE s.name = N'dbo' AND t.name = {tableName}
                         )
                         THEN 1
                         ELSE 0
                     END
                 AS int) AS [Value]
                 """)
            .SingleAsync(cancellationToken);

        return result == 1;
    }
}
