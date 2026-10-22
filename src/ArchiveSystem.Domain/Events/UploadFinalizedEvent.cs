namespace ArchiveSystem.Domain.Events
{
    public class UploadFinalizedEvent : DomainEvent
    {
        public Guid SessionId { get; }
        public long? FileId { get; }
        public long FileVersionId { get; }
        public string ObjectKey { get; }

        public UploadFinalizedEvent(Guid sessionId, long? fileId, long fileVersionId, string objectKey)
        {
            SessionId = sessionId;
            FileId = fileId;
            FileVersionId = fileVersionId;
            ObjectKey = objectKey;
        }
    }
}
