namespace ArchiveSystem.Application.Features.Reports.Models
{
    public sealed class ActivityReportFilter
    {
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }
        public long? ActorUserId { get; set; }
        public string? ActionType { get; set; }
        public long? ClassificationId { get; set; }
        public long? CaseId { get; set; }
        public long? FileId { get; set; }
        public long? TagId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string SortBy { get; set; } = "timestamp";
        public bool SortDescending { get; set; } = true;
    }

    public sealed class ActivityReportItemDto
    {
        public long Id { get; set; }
        public DateTime Timestamp { get; set; }
        public long ActorUserId { get; set; }
        public string? ActorName { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public long? ClassificationId { get; set; }
        public long? CaseId { get; set; }
        public long? FileId { get; set; }
        public long? FileVersionId { get; set; }
        public long? TagId { get; set; }
        public string? Ip { get; set; }
        public string? UserAgent { get; set; }
        public string? BeforeJson { get; set; }
        public string? AfterJson { get; set; }
    }

    public sealed class ActivityReportContext
    {
        public bool IsAdmin { get; set; }
        public IReadOnlyCollection<long> AllowedClassificationIds { get; set; } = Array.Empty<long>();
        public IReadOnlyCollection<long> AllowedTagIds { get; set; } = Array.Empty<long>();
    }

    public sealed class ReportSummaryDto
    {
        public IReadOnlyList<ClassificationCountDto> CountsByClassification { get; set; } = Array.Empty<ClassificationCountDto>();
        public IReadOnlyList<ActionCountDto> CountsByActionType { get; set; } = Array.Empty<ActionCountDto>();
        public long TotalFiles { get; set; }
        public long TotalStorageBytes { get; set; }
    }

    public sealed class ClassificationCountDto
    {
        public long ClassificationId { get; set; }
        public long Count { get; set; }
    }

    public sealed class ActionCountDto
    {
        public string ActionType { get; set; } = string.Empty;
        public long Count { get; set; }
    }
}
