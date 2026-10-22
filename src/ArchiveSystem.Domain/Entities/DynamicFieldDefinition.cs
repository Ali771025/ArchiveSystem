using ArchiveSystem.Domain.Enums;

namespace ArchiveSystem.Domain.Entities
{
    public class DynamicFieldDefinition
    {
        public Guid Id { get; private set; }
        public Guid GroupId { get; private set; }
        public string Key { get; private set; }
        public string Label { get; private set; }
        public DynamicFieldType FieldType { get; private set; }
        public bool IsRequired { get; private set; }
        public decimal? MinNumber { get; private set; }
        public decimal? MaxNumber { get; private set; }
        public int? MinLength { get; private set; }
        public int? MaxLength { get; private set; }
        public string? Regex { get; private set; }
        public string? ErrorMessage { get; private set; }
        public bool IsMultiSelect { get; private set; }
        public bool ShowAsFilter { get; private set; }
        public DynamicFieldFilterType FilterType { get; private set; }
        public int Order { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        public DynamicFieldGroup Group { get; private set; } = null!;
        public ICollection<DynamicFieldOption> Options { get; private set; } = new List<DynamicFieldOption>();

        private DynamicFieldDefinition()
        {
            Key = string.Empty;
            Label = string.Empty;
            FilterType = DynamicFieldFilterType.Contains;
        }

        public DynamicFieldDefinition(
            Guid id,
            Guid groupId,
            string key,
            string label,
            DynamicFieldType fieldType,
            bool isRequired,
            decimal? minNumber,
            decimal? maxNumber,
            int? minLength,
            int? maxLength,
            string? regex,
            bool isMultiSelect,
            bool showAsFilter,
            DynamicFieldFilterType filterType,
            int order,
            bool isActive,
            string? errorMessage = null)
        {
            Id = id == Guid.Empty ? Guid.NewGuid() : id;
            GroupId = groupId;
            Key = key;
            Label = label;
            FieldType = fieldType;
            IsRequired = isRequired;
            MinNumber = minNumber;
            MaxNumber = maxNumber;
            MinLength = minLength;
            MaxLength = maxLength;
            Regex = regex;
            ErrorMessage = NormalizeOptional(errorMessage, 500);
            IsMultiSelect = isMultiSelect;
            ShowAsFilter = showAsFilter;
            FilterType = filterType;
            Order = order;
            IsActive = isActive;
            CreatedAt = DateTime.UtcNow;
        }

        public void Update(
            string key,
            string label,
            DynamicFieldType fieldType,
            bool isRequired,
            decimal? minNumber,
            decimal? maxNumber,
            int? minLength,
            int? maxLength,
            string? regex,
            bool isMultiSelect,
            bool showAsFilter,
            DynamicFieldFilterType filterType,
            int order,
            bool isActive,
            string? errorMessage = null)
        {
            Key = key;
            Label = label;
            FieldType = fieldType;
            IsRequired = isRequired;
            MinNumber = minNumber;
            MaxNumber = maxNumber;
            MinLength = minLength;
            MaxLength = maxLength;
            Regex = regex;
            ErrorMessage = NormalizeOptional(errorMessage, 500);
            IsMultiSelect = isMultiSelect;
            ShowAsFilter = showAsFilter;
            FilterType = filterType;
            Order = order;
            IsActive = isActive;
            UpdatedAt = DateTime.UtcNow;
        }

        private static string? NormalizeOptional(string? value, int maxLength)
        {
            var normalized = value?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return null;
            }

            return normalized.Length <= maxLength
                ? normalized
                : normalized[..maxLength];
        }
    }
}
