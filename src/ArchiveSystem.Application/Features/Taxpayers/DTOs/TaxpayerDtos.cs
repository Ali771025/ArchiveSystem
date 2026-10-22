using ArchiveSystem.Domain.Enums;

namespace ArchiveSystem.Application.Features.Taxpayers.DTOs
{
    public sealed record TaxpayerListItemDto(
        Guid Id,
        TaxpayerType TaxpayerType,
        string? FirstName,
        string? LastName,
        string? NationalId,
        string? CompanyName,
        string? NationalCompanyId,
        string? Mobile);

    public sealed record TaxpayerDetailsDto(
        Guid Id,
        TaxpayerType TaxpayerType,
        string? FirstName,
        string? LastName,
        string? NationalId,
        string? CompanyName,
        string? NationalCompanyId,
        string? IdentityNumber,
        string? Mobile,
        string? LandlinePhone,
        string? PostalCode,
        string? Address,
        DateTime CreatedAt,
        DateTime? ModifiedAt);
}
