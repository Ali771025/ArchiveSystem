namespace ArchiveSystem.Domain.Entities
{
    public class CaseFieldValueDate
    {
        public long CaseId { get; private set; }
        public Guid FieldId { get; private set; }
        public DateTime Value { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        public Case Case { get; private set; } = null!;
        public DynamicFieldDefinition Field { get; private set; } = null!;

        private CaseFieldValueDate()
        {
        }

        public CaseFieldValueDate(long caseId, Guid fieldId, DateTime value)
        {
            CaseId = caseId;
            FieldId = fieldId;
            Value = value;
            CreatedAt = DateTime.UtcNow;
        }

        public void UpdateValue(DateTime value)
        {
            Value = value;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
