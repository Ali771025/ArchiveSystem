namespace ArchiveSystem.Domain.Events
{
    public class FileVersionCreatedEvent : DomainEvent
    {
        public long FileId { get; }
        public long FileVersionId { get; }
        public string StoragePath { get; }
        public string ContentType { get; }
        public long Size { get; }

        public FileVersionCreatedEvent(long fileId, long fileVersionId, string storagePath, string contentType, long size)
        {
            FileId = fileId;
            FileVersionId = fileVersionId;
            StoragePath = storagePath;
            ContentType = contentType;
            Size = size;
        }
    }
}
