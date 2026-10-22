// src/ArchiveSystem.Domain/Events/ThumbnailGeneratedEvent.cs
namespace ArchiveSystem.Domain.Events
{
    public class ThumbnailGeneratedEvent : DomainEvent
    {
        public long FileId { get; }
        public long FileVersionId { get; }
        public int BytesLength { get; }
        public string ContentType { get; }
        public int Width { get; }
        public int Height { get; }

        public ThumbnailGeneratedEvent(long fileId, long fileVersionId, int bytesLength, string contentType, int width, int height)
        {
            FileId = fileId;
            FileVersionId = fileVersionId;
            BytesLength = bytesLength;
            ContentType = contentType;
            Width = width;
            Height = height;
        }
    }
}
