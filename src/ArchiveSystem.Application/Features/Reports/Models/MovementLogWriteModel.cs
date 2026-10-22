namespace ArchiveSystem.Application.Features.Reports.Models
{
    public sealed class MovementLogWriteModel
    {
        public DateTime? Timestamp { get; set; }
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
        public string? BeforeJson { get; set; }
        public string? AfterJson { get; set; }
        public string? Ip { get; set; }
        public string? UserAgent { get; set; }
    }
}
