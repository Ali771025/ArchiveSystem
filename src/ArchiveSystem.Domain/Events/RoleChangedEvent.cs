namespace ArchiveSystem.Domain.Events
{
    public class RoleChangedEvent : DomainEvent
    {
        public RoleChangedEvent(long roleId, string changeType)
        {
            RoleId = roleId;
            ChangeType = changeType;
        }

        public long RoleId { get; }
        public string ChangeType { get; }
    }
}
