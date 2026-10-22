namespace ArchiveSystem.Presentation.Api.Models.Cases
{
    public sealed class UpsertCaseDynamicFieldsRequest
    {
        public List<CaseDynamicFieldValueRequest> Values { get; set; } = new();
    }

    public sealed class CaseDynamicFieldValueRequest
    {
        public Guid FieldId { get; set; }
        public string? StringValue { get; set; }
        public decimal? NumberValue { get; set; }
        public DateTime? DateValue { get; set; }
        public bool? BoolValue { get; set; }
        public List<Guid>? OptionIds { get; set; }
    }
}
