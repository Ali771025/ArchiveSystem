// src/ArchiveSystem.Domain/BaseEntity.cs
using System.ComponentModel.DataAnnotations.Schema;
using ArchiveSystem.Domain.Events;

namespace ArchiveSystem.Domain.Entities
{
    public abstract class BaseEntity<TId>
    {
        private readonly List<DomainEvent> _domainEvents = new();

        public TId Id { get; protected set; } = default!;
        public DateTime CreatedAt { get; protected set; }
        public DateTime? ModifiedAt { get; protected set; }

        [NotMapped]
        public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected BaseEntity()
        {
            CreatedAt = DateTime.UtcNow;
        }

        protected BaseEntity(TId id) : this()
        {
            Id = id;
        }

        public void MarkAsModified()
        {
            ModifiedAt = DateTime.UtcNow;
        }

        protected void AddDomainEvent(DomainEvent domainEvent)
        {
            if (domainEvent == null)
                throw new ArgumentNullException(nameof(domainEvent));

            _domainEvents.Add(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}
