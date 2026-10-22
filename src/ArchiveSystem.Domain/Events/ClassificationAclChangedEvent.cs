namespace ArchiveSystem.Domain.Events
{
    public class ClassificationAclChangedEvent : DomainEvent
    {
        public ClassificationAclChangedEvent(long classificationId)
        {
            ClassificationId = classificationId;
        }

        public long ClassificationId { get; }
    }
}
