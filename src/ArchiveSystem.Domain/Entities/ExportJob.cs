using ArchiveSystem.Domain.Enums;

namespace ArchiveSystem.Domain.Entities
{
    public class ExportJob : BaseEntity<Guid>
    {
        public long CreatedByUserId { get; private set; }
        public ExportJobType Type { get; private set; }
        public ExportJobStatus Status { get; private set; }
        public int? ProgressPercent { get; private set; }
        public string? RequestJson { get; private set; }
        public string? ResultObjectKey { get; private set; }
        public string? ErrorMessage { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public DateTime? StartedAt { get; private set; }
        public DateTime? CompletedAt { get; private set; }

        private ExportJob() : base()
        {
            Id = Guid.NewGuid();
            Status = ExportJobStatus.Pending;
        }

        public ExportJob(
            Guid id,
            long createdByUserId,
            ExportJobType type,
            string? requestJson,
            DateTime expiresAt)
            : base(id == Guid.Empty ? Guid.NewGuid() : id)
        {
            if (createdByUserId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(createdByUserId), "Creator user id must be positive.");
            }

            CreatedByUserId = createdByUserId;
            Type = type;
            Status = ExportJobStatus.Pending;
            RequestJson = requestJson;
            ExpiresAt = expiresAt;
        }

        public void MarkRunning()
        {
            Status = ExportJobStatus.Running;
            StartedAt = DateTime.UtcNow;
            ProgressPercent = 0;
            ErrorMessage = null;
            ResultObjectKey = null;
            MarkAsModified();
        }

        public void SetProgress(int progressPercent)
        {
            ProgressPercent = Math.Clamp(progressPercent, 0, 100);
            MarkAsModified();
        }

        public void MarkCompleted(string resultObjectKey, DateTime? expiresAt = null)
        {
            if (string.IsNullOrWhiteSpace(resultObjectKey))
            {
                throw new ArgumentException("Result object key is required.", nameof(resultObjectKey));
            }

            Status = ExportJobStatus.Completed;
            ProgressPercent = 100;
            CompletedAt = DateTime.UtcNow;
            ErrorMessage = null;
            ResultObjectKey = resultObjectKey;
            if (expiresAt.HasValue)
            {
                ExpiresAt = expiresAt.Value;
            }
            MarkAsModified();
        }

        public void MarkFailed(string errorMessage)
        {
            Status = ExportJobStatus.Failed;
            CompletedAt = DateTime.UtcNow;
            ErrorMessage = errorMessage;
            MarkAsModified();
        }

        public void MarkExpired()
        {
            Status = ExportJobStatus.Expired;
            MarkAsModified();
        }
    }
}
