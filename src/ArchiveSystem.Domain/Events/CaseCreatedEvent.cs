// src/ArchiveSystem.Domain/Events/CaseCreatedEvent.cs
namespace ArchiveSystem.Domain.Events
{
    public class CaseCreatedEvent : DomainEvent
    {
        public long CaseId { get; }
        public string CaseNumber { get; }
        public string Title { get; }
        public long ClassificationId { get; }

        public CaseCreatedEvent(long caseId, string caseNumber, string title, long classificationId)
        {
            CaseId = caseId;
            CaseNumber = caseNumber;
            Title = title;
            ClassificationId = classificationId;
        }
    }
}