// src/ArchiveSystem.Domain/Events/OcrCompletedEvent.cs
namespace ArchiveSystem.Domain.Events
{
    public class OcrCompletedEvent : DomainEvent
    {
        public long FileId { get; }
        public long FileVersionId { get; }
        public string Language { get; }
        public int WordCount { get; }
        public bool HasText { get; }

        public OcrCompletedEvent(long fileId, long fileVersionId, string language, int wordCount, bool hasText)
        {
            FileId = fileId;
            FileVersionId = fileVersionId;
            Language = language;
            WordCount = wordCount;
            HasText = hasText;
        }
    }
}
