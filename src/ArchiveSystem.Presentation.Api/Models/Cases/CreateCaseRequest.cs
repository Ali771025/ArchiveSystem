namespace ArchiveSystem.Presentation.Api.Models.Cases
{
    public sealed class CreateCaseRequest
    {
        public long ClassificationId { get; set; }
        public string? ExternalCaseNumber { get; set; }
        public string Address { get; set; } = string.Empty;
        public string? PostalCode { get; set; }
        public List<CaseOwnerRequest> Owners { get; set; } = new();
    }

    public sealed class CaseOwnerRequest
    {
        public long Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string NationalCode { get; set; } = string.Empty;
        public string? TaxpayerId { get; set; }
        public string Phone { get; set; } = string.Empty;
    }
}
