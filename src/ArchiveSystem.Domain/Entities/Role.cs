using ArchiveSystem.Domain.Events;

namespace ArchiveSystem.Domain.Entities
{
    public class Role : BaseEntity<long>, IAggregateRoot
    {
        public string Name { get; private set; }
        public bool IsSystem { get; private set; }

        private Role() : base()
        {
            Name = string.Empty;
        }

        public Role(long id, string name, bool isSystem = false) : base(id)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Role name is required.", nameof(name));
            }

            Name = name.Trim();
            IsSystem = isSystem;
        }

        public void Rename(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Role name is required.", nameof(name));
            }

            Name = name.Trim();
            MarkAsModified();
            AddDomainEvent(new RoleChangedEvent(Id, "Renamed"));
        }
    }
}
