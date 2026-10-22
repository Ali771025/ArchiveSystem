// src/ArchiveSystem.Domain/Events/FileDownloadedEvent.cs
namespace ArchiveSystem.Domain.Events
{
    public class FileDownloadedEvent : DomainEvent
    {
        public long UserId { get; }
        public long FileId { get; }
        public string IpAddress { get; }
        public string UserAgent { get; }

        public FileDownloadedEvent(long userId, long fileId, string ipAddress, string userAgent)
        {
            UserId = userId;
            FileId = fileId;
            IpAddress = ipAddress;
            UserAgent = userAgent;
        }
    }
}
