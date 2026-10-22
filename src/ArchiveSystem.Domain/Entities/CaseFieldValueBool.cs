namespace ArchiveSystem.Domain.Entities
{
    public class CaseFieldValueBool
    {
        public long CaseId { get; private set; }
        public Guid FieldId { get; private set; }
        public bool Value { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        public Case Case { get; private set; } = null!;
        public DynamicFieldDefinition Field { get; private set; } = null!;

        private CaseFieldValueBool()
        {
        }

        public CaseFieldValueBool(long caseId, Guid fieldId, bool value)
        {
            CaseId = caseId;
            FieldId = fieldId;
            Value = value;
            CreatedAt = DateTime.UtcNow;
        }

        public void UpdateValue(bool value)
        {
            Value = value;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
