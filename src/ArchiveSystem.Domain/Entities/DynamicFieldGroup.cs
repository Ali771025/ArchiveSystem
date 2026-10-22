namespace ArchiveSystem.Domain.Entities
{
    public class DynamicFieldGroup
    {
        public Guid Id { get; private set; }
        public Guid TemplateId { get; private set; }
        public string Title { get; private set; }
        public int Order { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        public DynamicTemplate Template { get; private set; } = null!;
        public ICollection<DynamicFieldDefinition> Fields { get; private set; } = new List<DynamicFieldDefinition>();

        private DynamicFieldGroup()
        {
            Title = string.Empty;
        }

        public DynamicFieldGroup(Guid id, Guid templateId, string title, int order)
        {
            Id = id == Guid.Empty ? Guid.NewGuid() : id;
            TemplateId = templateId;
            Title = title;
            Order = order;
            CreatedAt = DateTime.UtcNow;
        }

        public void Update(string title, int order)
        {
            Title = title;
            Order = order;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
