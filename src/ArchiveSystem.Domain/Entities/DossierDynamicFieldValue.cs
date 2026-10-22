using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Interfaces;

namespace ArchiveSystem.Domain.Entities
{
    public class DossierDynamicFieldValue : BaseEntity<long>, ISoftDeletable
    {
        public long DossierId { get; private set; }
        public Guid DynamicFieldId { get; private set; }
        public DossierDynamicFieldType FieldType { get; private set; }

        public string? TextValue { get; private set; }
        public int? IntegerValue { get; private set; }
        public decimal? DecimalValue { get; private set; }
        public DateOnly? DateValue { get; private set; }
        public DateTime? DateTimeValue { get; private set; }
        public TimeSpan? TimeValue { get; private set; }
        public bool? CheckboxValue { get; private set; }
        public string? OptionValue { get; private set; }

        public DateTime? DeletedAt { get; private set; }
        public bool IsDeleted => DeletedAt.HasValue;

        public Dossier Dossier { get; private set; } = null!;
        public DossierDynamicField DynamicField { get; private set; } = null!;

        private DossierDynamicFieldValue() : base()
        {
        }

        public DossierDynamicFieldValue(
            long id,
            long dossierId,
            Guid dynamicFieldId,
            DossierDynamicFieldType fieldType) : base(id)
        {
            if (dossierId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(dossierId), "DossierId must be greater than zero.");
            }

            if (dynamicFieldId == Guid.Empty)
            {
                throw new ArgumentException("DynamicFieldId cannot be empty.", nameof(dynamicFieldId));
            }

            DossierId = dossierId;
            DynamicFieldId = dynamicFieldId;
            FieldType = fieldType;
        }

        public void SetValue(
            string? textValue = null,
            int? integerValue = null,
            decimal? decimalValue = null,
            DateOnly? dateValue = null,
            DateTime? dateTimeValue = null,
            TimeSpan? timeValue = null,
            bool? checkboxValue = null,
            string? optionValue = null)
        {
            TextValue = null;
            IntegerValue = null;
            DecimalValue = null;
            DateValue = null;
            DateTimeValue = null;
            TimeValue = null;
            CheckboxValue = null;
            OptionValue = null;

            switch (FieldType)
            {
                case DossierDynamicFieldType.Text:
                case DossierDynamicFieldType.MultilineText:
                    TextValue = NormalizeOptional(textValue, 4000);
                    break;
                case DossierDynamicFieldType.Dropdown:
                    OptionValue = NormalizeOptional(optionValue, 150);
                    break;
                case DossierDynamicFieldType.Integer:
                    IntegerValue = integerValue;
                    break;
                case DossierDynamicFieldType.Decimal:
                    DecimalValue = decimalValue;
                    break;
                case DossierDynamicFieldType.Date:
                    DateValue = dateValue;
                    break;
                case DossierDynamicFieldType.DateTime:
                    DateTimeValue = dateTimeValue;
                    break;
                case DossierDynamicFieldType.Time:
                    TimeValue = timeValue;
                    break;
                case DossierDynamicFieldType.Checkbox:
                    CheckboxValue = checkboxValue;
                    break;
                default:
                    throw new NotSupportedException($"Unsupported field type '{FieldType}'.");
            }

            MarkAsModified();
        }

        public void SoftDelete()
        {
            if (IsDeleted)
            {
                return;
            }

            DeletedAt = DateTime.UtcNow;
            MarkAsModified();
        }

        public void Restore()
        {
            if (!IsDeleted)
            {
                return;
            }

            DeletedAt = null;
            MarkAsModified();
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
