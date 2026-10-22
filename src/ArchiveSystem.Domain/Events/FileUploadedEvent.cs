// src/ArchiveSystem.Domain/Events/FileUploadedEvent.cs
namespace ArchiveSystem.Domain.Events
{
    public class FileUploadedEvent : DomainEvent
    {
        public long FileId { get; }
        public string FileName { get; }
        public long FileSize { get; }
        public string StoragePath { get; }

        public FileUploadedEvent(long fileId, string fileName, long fileSize, string storagePath)
        {
            FileId = fileId;
            FileName = fileName;
            FileSize = fileSize;
            StoragePath = storagePath;
        }
    }
}