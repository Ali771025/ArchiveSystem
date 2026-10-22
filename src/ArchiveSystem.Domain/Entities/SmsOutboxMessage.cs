namespace ArchiveSystem.Domain.Entities
{
    public enum SmsOutboxStatus
    {
        Pending = 0,
        Sent = 1,
        Failed = 2,
        DeadLetter = 3
    }

    public sealed class SmsOutboxMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public long UserId { get; set; }
        public string MobileNumber { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public string Provider { get; set; } = "MeliPayamak";
        public string PatternCode { get; set; } = string.Empty;
        public string PayloadJson { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string? ClientIpAddress { get; set; }
        public string? UserAgent { get; set; }
        public SmsOutboxStatus Status { get; set; } = SmsOutboxStatus.Pending;
        public int AttemptCount { get; set; }
        public int MaxAttempts { get; set; } = 5;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? SentAtUtc { get; set; }
        public DateTime? NextAttemptAtUtc { get; set; }
        public string? LastError { get; set; }
    }
}
