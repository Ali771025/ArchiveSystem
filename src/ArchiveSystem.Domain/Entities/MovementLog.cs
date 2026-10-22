namespace ArchiveSystem.Domain.Entities
{
    public class MovementLog : BaseEntity<long>
    {
        public DateTime Timestamp { get; private set; }
        public long ActorUserId { get; private set; }
        public string? ActorName { get; private set; }
        public string ActionType { get; private set; }
        public string EntityType { get; private set; }
        public string EntityId { get; private set; }
        public long? ClassificationId { get; private set; }
        public long? CaseId { get; private set; }
        public long? FileId { get; private set; }
        public long? FileVersionId { get; private set; }
        public long? TagId { get; private set; }
        public string? BeforeJson { get; private set; }
        public string? AfterJson { get; private set; }
        public string? Ip { get; private set; }
        public string? UserAgent { get; private set; }

        private MovementLog() : base()
        {
            Timestamp = DateTime.UtcNow;
            ActionType = string.Empty;
            EntityType = string.Empty;
            EntityId = string.Empty;
        }

        public MovementLog(
            long actorUserId,
            string actionType,
            string entityType,
            string entityId,
            DateTime? timestamp = null,
            string? actorName = null,
            long? classificationId = null,
            long? caseId = null,
            long? fileId = null,
            long? fileVersionId = null,
            long? tagId = null,
            string? beforeJson = null,
            string? afterJson = null,
            string? ip = null,
            string? userAgent = null)
            : base(0)
        {
            if (string.IsNullOrWhiteSpace(actionType))
            {
                throw new ArgumentException("Action type is required.", nameof(actionType));
            }

            if (string.IsNullOrWhiteSpace(entityType))
            {
                throw new ArgumentException("Entity type is required.", nameof(entityType));
            }

            if (string.IsNullOrWhiteSpace(entityId))
            {
                throw new ArgumentException("Entity id is required.", nameof(entityId));
            }

            Timestamp = timestamp ?? DateTime.UtcNow;
            ActorUserId = actorUserId;
            ActorName = actorName;
            ActionType = actionType;
            EntityType = entityType;
            EntityId = entityId;
            ClassificationId = classificationId;
            CaseId = caseId;
            FileId = fileId;
            FileVersionId = fileVersionId;
            TagId = tagId;
            BeforeJson = beforeJson;
            AfterJson = afterJson;
            Ip = ip;
            UserAgent = userAgent;
        }
    }
}
