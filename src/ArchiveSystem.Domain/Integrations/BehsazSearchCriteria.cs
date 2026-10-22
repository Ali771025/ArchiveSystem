using ArchiveSystem.Domain.Enums;

namespace ArchiveSystem.Domain.Integrations
{
    public sealed class BehsazSearchCriteria
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
        public int InstaurationFilterType { get; init; } = 1;
        public int? Area { get; init; }
        public int? Zone { get; init; }
        public int? Block { get; init; }
        public int? Land { get; init; }
        public int? Building { get; init; }
        public int? Apartment { get; init; }
        public int? Business { get; init; }
        public string? SystemNumber { get; init; }
        public string? FileNumber { get; init; }
        public decimal? GhateCode { get; init; }
        public decimal? RegistrationPlateMain { get; init; }
        public decimal? RegistrationPlateSub { get; init; }
        public string? Address { get; init; }
        public int? PropertyStatus { get; init; }
        public bool? GisIndicated { get; init; }
        public TaxpayerGroup? TaxpayerGroup { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? NationalCode { get; init; }
        public string? CompanyName { get; init; }
        public string? NationalCompanyId { get; init; }
        public string? RegisterNumber { get; init; }
        public int? UserCode { get; init; }
    }
}
