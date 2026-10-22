using System.ComponentModel.DataAnnotations;

namespace ArchiveSystem.Application.Features.Reports.Models;

public sealed class AuditFilterDto : PagingRequestDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? UserId { get; set; }
    public List<string>? EntityTypes { get; set; }
    public List<string>? ActionTypes { get; set; }
}

public sealed class SecurityLogFilterDto : PagingRequestDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? ActorUserId { get; set; }
    public string? TargetUserId { get; set; }
    public List<string>? EventTypes { get; set; }
}

public abstract class PagingRequestDto
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 200)]
    public int PageSize { get; set; } = 50;

    public string? SortBy { get; set; }
    public string? SortDirection { get; set; }
}

public sealed class AuditLogResponseDto
{
    public long AuditId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime ActionDate { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? AffectedColumns { get; set; }
}

public sealed class SecurityLogResponseDto
{
    public int SecurityLogId { get; set; }
    public string? EventType { get; set; }
    public string? ActorUserId { get; set; }
    public string? ActorUserName { get; set; }
    public string? TargetUserId { get; set; }
    public string? TargetUserName { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ClientIpAddress { get; set; }
    public string? Level { get; set; }
    public DateTime ActionDate { get; set; }
    public string? SecurityMetadata { get; set; }
}

public sealed class PagedResultDto<TItem>
{
    public IReadOnlyCollection<TItem> Items { get; init; } = Array.Empty<TItem>();
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
}

public sealed class AuditFilterOptionsDto
{
    public IReadOnlyList<string> EntityTypes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ActionTypes { get; init; } = Array.Empty<string>();
}
