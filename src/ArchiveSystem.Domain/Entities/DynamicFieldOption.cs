namespace ArchiveSystem.Domain.Entities
{
    public class DynamicFieldOption
    {
        public Guid Id { get; private set; }
        public Guid FieldId { get; private set; }
        public string Value { get; private set; }
        public string Label { get; private set; }
        public int Order { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        public DynamicFieldDefinition Field { get; private set; } = null!;

        private DynamicFieldOption()
        {
            Value = string.Empty;
            Label = string.Empty;
        }

        public DynamicFieldOption(Guid id, Guid fieldId, string value, string label, int order, bool isActive)
        {
            Id = id == Guid.Empty ? Guid.NewGuid() : id;
            FieldId = fieldId;
            Value = value;
            Label = label;
            Order = order;
            IsActive = isActive;
            CreatedAt = DateTime.UtcNow;
        }

        public void Update(string value, string label, int order, bool isActive)
        {
            Value = value;
            Label = label;
            Order = order;
            IsActive = isActive;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
