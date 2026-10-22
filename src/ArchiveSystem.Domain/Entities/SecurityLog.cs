namespace ArchiveSystem.Domain.Entities
{
    public sealed class SecurityLog
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Level { get; set; }
        public DateTime ActionDate { get; set; }
        public string? EventType { get; set; }
        public string? ActorUserId { get; set; }
        public string? TargetUserId { get; set; }
        public string? ClientIpAddress { get; set; }
        public string? SecurityMetadata { get; set; }
    }
}
