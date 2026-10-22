// src/ArchiveSystem.Domain/Events/FileDeletedEvent.cs
namespace ArchiveSystem.Domain.Events
{
    public class FileDeletedEvent : DomainEvent
    {
        public long UserId { get; }
        public long FileId { get; }
        public string Reason { get; }
        public string IpAddress { get; }

        public FileDeletedEvent(long userId, long fileId, string reason, string ipAddress)
        {
            UserId = userId;
            FileId = fileId;
            Reason = reason;
            IpAddress = ipAddress;
        }
    }
}
