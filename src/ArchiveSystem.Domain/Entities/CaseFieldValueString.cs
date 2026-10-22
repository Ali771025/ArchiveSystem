namespace ArchiveSystem.Domain.Entities
{
    public class CaseFieldValueString
    {
        public long CaseId { get; private set; }
        public Guid FieldId { get; private set; }
        public string Value { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        public Case Case { get; private set; } = null!;
        public DynamicFieldDefinition Field { get; private set; } = null!;

        private CaseFieldValueString()
        {
            Value = string.Empty;
        }

        public CaseFieldValueString(long caseId, Guid fieldId, string value)
        {
            CaseId = caseId;
            FieldId = fieldId;
            Value = value;
            CreatedAt = DateTime.UtcNow;
        }

        public void UpdateValue(string value)
        {
            Value = value;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
