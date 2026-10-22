using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Interfaces;

namespace ArchiveSystem.Domain.Entities
{
    public class DossierDynamicField : BaseEntity<Guid>, ISoftDeletable
    {
        public Guid GroupId { get; private set; }
        public string Title { get; private set; }
        public string ApiName { get; private set; }
        public DossierDynamicFieldType FieldType { get; private set; }
        public string? ValidationRegex { get; private set; }
        public string? ErrorMessage { get; private set; }
        public int DisplayOrder { get; private set; }
        public DossierDynamicFieldScope Scope { get; private set; }
        public long? ClassificationId { get; private set; }
        public bool IsActive { get; private set; }

        public DateTime? DeletedAt { get; private set; }
        public bool IsDeleted => DeletedAt.HasValue;

        public DossierDynamicFieldGroup Group { get; private set; } = null!;
        public Classification? Classification { get; private set; }
        public ICollection<DossierDynamicFieldOption> Options { get; private set; } = new List<DossierDynamicFieldOption>();
        public ICollection<DossierDynamicFieldValue> Values { get; private set; } = new List<DossierDynamicFieldValue>();

        private DossierDynamicField()
        {
            Title = string.Empty;
            ApiName = string.Empty;
        }

        public DossierDynamicField(
            Guid id,
            Guid groupId,
            string title,
            string apiName,
            DossierDynamicFieldType fieldType,
            int displayOrder,
            DossierDynamicFieldScope scope,
            long? classificationId = null,
            string? validationRegex = null,
            string? errorMessage = null,
            bool isActive = true) : base(id == Guid.Empty ? Guid.NewGuid() : id)
        {
            if (groupId == Guid.Empty)
            {
                throw new ArgumentException("GroupId cannot be empty.", nameof(groupId));
            }

            ValidateScope(scope, classificationId);

            GroupId = groupId;
            Title = RequireText(title, nameof(title), 150);
            ApiName = RequireText(apiName, nameof(apiName), 80);
            FieldType = fieldType;
            DisplayOrder = displayOrder;
            Scope = scope;
            ClassificationId = classificationId;
            ValidationRegex = NormalizeOptional(validationRegex, 500);
            ErrorMessage = NormalizeOptional(errorMessage, 500);
            IsActive = isActive;
        }

        public void UpdateMetadata(
            string title,
            string apiName,
            DossierDynamicFieldType fieldType,
            int displayOrder,
            DossierDynamicFieldScope scope,
            long? classificationId,
            string? validationRegex,
            string? errorMessage,
            bool isActive)
        {
            ValidateScope(scope, classificationId);

            Title = RequireText(title, nameof(title), 150);
            ApiName = RequireText(apiName, nameof(apiName), 80);
            FieldType = fieldType;
            DisplayOrder = displayOrder;
            Scope = scope;
            ClassificationId = classificationId;
            ValidationRegex = NormalizeOptional(validationRegex, 500);
            ErrorMessage = NormalizeOptional(errorMessage, 500);
            IsActive = isActive;
            MarkAsModified();
        }

        public void SoftDelete()
        {
            if (IsDeleted)
            {
                return;
            }

            IsActive = false;
            DeletedAt = DateTime.UtcNow;
            MarkAsModified();
        }

        public void Restore()
        {
            if (!IsDeleted)
            {
                return;
            }

            IsActive = true;
            DeletedAt = null;
            MarkAsModified();
        }

        private static void ValidateScope(DossierDynamicFieldScope scope, long? classificationId)
        {
            if (scope == DossierDynamicFieldScope.Public && classificationId.HasValue)
            {
                throw new ArgumentException("Public fields cannot have a classification id.", nameof(classificationId));
            }

            if (scope == DossierDynamicFieldScope.ClassificationSpecific && (!classificationId.HasValue || classificationId.Value <= 0))
            {
                throw new ArgumentException("Classification specific fields must have a valid classification id.", nameof(classificationId));
            }
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

        private static string? NormalizeOptional(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Trim();
            if (normalized.Length > maxLength)
            {
                throw new ArgumentException($"Value must be {maxLength} characters or less.", nameof(value));
            }

            return normalized;
        }
    }
}
