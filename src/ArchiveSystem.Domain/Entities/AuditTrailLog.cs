namespace ArchiveSystem.Domain.Entities
{
    public sealed class AuditTrailLog
    {
        public long AuditId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string RecordId { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public DateTime ActionDate { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? AffectedColumns { get; set; }
    }
}
