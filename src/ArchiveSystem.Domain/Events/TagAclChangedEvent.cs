namespace ArchiveSystem.Domain.Events
{
    public class TagAclChangedEvent : DomainEvent
    {
        public TagAclChangedEvent(long tagId)
        {
            TagId = tagId;
        }

        public long TagId { get; }
    }
}
