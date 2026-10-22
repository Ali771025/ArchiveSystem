namespace ArchiveSystem.Domain.Entities
{
    public class DynamicTemplate
    {
        public Guid Id { get; private set; }
        public long ClassificationId { get; private set; }
        public string Name { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        public Classification Classification { get; private set; } = null!;
        public ICollection<DynamicFieldGroup> Groups { get; private set; } = new List<DynamicFieldGroup>();

        private DynamicTemplate()
        {
            Name = string.Empty;
        }

        public DynamicTemplate(Guid id, long classificationId, string name, bool isActive = true)
        {
            Id = id == Guid.Empty ? Guid.NewGuid() : id;
            ClassificationId = classificationId;
            Name = name;
            IsActive = isActive;
            CreatedAt = DateTime.UtcNow;
        }

        public void UpdateMetadata(string name, bool isActive)
        {
            Name = name;
            IsActive = isActive;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
