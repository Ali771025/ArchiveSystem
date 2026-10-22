namespace ArchiveSystem.Presentation.Api.Models.Cases
{
    public sealed class UpdateCaseRequest
    {
        public long? ClassificationId { get; set; }
        public string? ExternalCaseNumber { get; set; }
        public string Address { get; set; } = string.Empty;
        public string? PostalCode { get; set; }
        public List<CaseOwnerRequest> Owners { get; set; } = new();
    }
}
