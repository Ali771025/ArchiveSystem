// src/ArchiveSystem.Domain/Events/DomainEvent.cs
using MediatR;

namespace ArchiveSystem.Domain.Events
{
    public abstract class DomainEvent : INotification
    {
        public DateTime OccurredOn { get; }
        public string EventType { get; }

        protected DomainEvent()
        {
            OccurredOn = DateTime.UtcNow;
            EventType = GetType().Name;
        }
    }
}
