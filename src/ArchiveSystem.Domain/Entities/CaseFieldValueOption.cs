namespace ArchiveSystem.Domain.Entities
{
    public class CaseFieldValueOption
    {
        public long CaseId { get; private set; }
        public Guid FieldId { get; private set; }
        public Guid OptionId { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public Case Case { get; private set; } = null!;
        public DynamicFieldDefinition Field { get; private set; } = null!;
        public DynamicFieldOption Option { get; private set; } = null!;

        private CaseFieldValueOption()
        {
        }

        public CaseFieldValueOption(long caseId, Guid fieldId, Guid optionId)
        {
            CaseId = caseId;
            FieldId = fieldId;
            OptionId = optionId;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
