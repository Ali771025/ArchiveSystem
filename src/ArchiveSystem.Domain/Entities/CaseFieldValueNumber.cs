namespace ArchiveSystem.Domain.Entities
{
    public class CaseFieldValueNumber
    {
        public long CaseId { get; private set; }
        public Guid FieldId { get; private set; }
        public decimal Value { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        public Case Case { get; private set; } = null!;
        public DynamicFieldDefinition Field { get; private set; } = null!;

        private CaseFieldValueNumber()
        {
        }

        public CaseFieldValueNumber(long caseId, Guid fieldId, decimal value)
        {
            CaseId = caseId;
            FieldId = fieldId;
            Value = value;
            CreatedAt = DateTime.UtcNow;
        }

        public void UpdateValue(decimal value)
        {
            Value = value;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
