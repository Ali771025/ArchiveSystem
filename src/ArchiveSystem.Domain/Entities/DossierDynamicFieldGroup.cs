using ArchiveSystem.Domain.Interfaces;

namespace ArchiveSystem.Domain.Entities
{
    public class DossierDynamicFieldGroup : BaseEntity<Guid>, ISoftDeletable
    {
        public string Title { get; private set; }
        public int DisplayOrder { get; private set; }

        public DateTime? DeletedAt { get; private set; }
        public bool IsDeleted => DeletedAt.HasValue;

        public ICollection<DossierDynamicField> Fields { get; private set; } = new List<DossierDynamicField>();

        private DossierDynamicFieldGroup()
        {
            Title = string.Empty;
        }

        public DossierDynamicFieldGroup(Guid id, string title, int displayOrder) : base(id == Guid.Empty ? Guid.NewGuid() : id)
        {
            Title = RequireText(title, nameof(title), 150);
            DisplayOrder = displayOrder;
        }

        public void Update(string title, int displayOrder)
        {
            Title = RequireText(title, nameof(title), 150);
            DisplayOrder = displayOrder;
            MarkAsModified();
        }

        public void SoftDelete()
        {
            DeletedAt = DateTime.UtcNow;
            MarkAsModified();
        }

        public void Restore()
        {
            DeletedAt = null;
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
