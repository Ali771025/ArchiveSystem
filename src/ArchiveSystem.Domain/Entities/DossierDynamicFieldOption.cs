namespace ArchiveSystem.Domain.Entities
{
    public class DossierDynamicFieldOption : BaseEntity<Guid>
    {
        public Guid DynamicFieldId { get; private set; }
        public string Value { get; private set; }
        public string Label { get; private set; }
        public int DisplayOrder { get; private set; }
        public bool IsActive { get; private set; }

        public DossierDynamicField DynamicField { get; private set; } = null!;

        private DossierDynamicFieldOption()
        {
            Value = string.Empty;
            Label = string.Empty;
        }

        public DossierDynamicFieldOption(
            Guid id,
            Guid dynamicFieldId,
            string value,
            string label,
            int displayOrder,
            bool isActive = true) : base(id == Guid.Empty ? Guid.NewGuid() : id)
        {
            if (dynamicFieldId == Guid.Empty)
            {
                throw new ArgumentException("DynamicFieldId cannot be empty.", nameof(dynamicFieldId));
            }

            DynamicFieldId = dynamicFieldId;
            Value = RequireText(value, nameof(value), 150);
            Label = RequireText(label, nameof(label), 150);
            DisplayOrder = displayOrder;
            IsActive = isActive;
        }

        public void Update(string value, string label, int displayOrder, bool isActive)
        {
            Value = RequireText(value, nameof(value), 150);
            Label = RequireText(label, nameof(label), 150);
            DisplayOrder = displayOrder;
            IsActive = isActive;
            MarkAsModified();
        }

        private static string RequireText(string? value, string paramName, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{paramName} cannot be empty.", paramName);
            }

            var normalized = value.Trim();
            if (normalized.Length > maxLength)
            {
                throw new ArgumentException($"{paramName} must be {maxLength} characters or less.", paramName);
            }

            return normalized;
        }
    }
}
