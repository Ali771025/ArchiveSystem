namespace ArchiveSystem.Application.Features.Cases.Models
{
    public sealed class CaseDetailsDto
    {
        public long Id { get; set; }
        public string CaseNumber { get; set; } = string.Empty;
        public string? ExternalCaseNumber { get; set; }
        public string? AvatarImagePath { get; set; }
        public string Address { get; set; } = string.Empty;
        public string? PostalCode { get; set; }
        public DateTime UpdatedAt { get; set; }
        public long ClassificationId { get; set; }
        public string ClassificationTitle { get; set; } = string.Empty;
        public List<string> OwnerNames { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public List<CaseOwnerDto> Owners { get; set; } = new();
        public List<CaseHistoryItemDto> History { get; set; } = new();
    }

    public sealed class CaseOwnerDto
    {
        public long Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string NationalCode { get; set; } = string.Empty;
        public string? TaxpayerId { get; set; }
        public string Phone { get; set; } = string.Empty;
    }

    public sealed class CaseHistoryItemDto
    {
        public long Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string ActorName { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
    }
}
