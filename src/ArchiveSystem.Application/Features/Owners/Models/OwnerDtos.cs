using ArchiveSystem.Domain.Enums;

namespace ArchiveSystem.Application.Features.Owners.Models;

public sealed class CreateOwnerDto
{
    public OwnerType OwnerType { get; set; } = OwnerType.Natural;
    public string? Mobile { get; set; }
    public string? PostalCode { get; set; }
    public string? Address { get; set; }
    public string? HomePhone { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? NationalCode { get; set; }
    public string? IdentityNumber { get; set; }
    public string? CompanyName { get; set; }
    public string? NationalId { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? TaxpayerId { get; set; }
}

public sealed class OwnerDto
{
    public long Id { get; set; }
    public OwnerType OwnerType { get; set; }
    public string? Mobile { get; set; }
    public string? PostalCode { get; set; }
    public string? Address { get; set; }
    public string? HomePhone { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? NationalCode { get; set; }
    public string? IdentityNumber { get; set; }
    public string? CompanyName { get; set; }
    public string? NationalId { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? TaxpayerId { get; set; }
}

public sealed class OwnerSearchItemDto
{
    public long Id { get; set; }
    public OwnerType OwnerType { get; set; }
    public string Label { get; set; } = string.Empty;
    public string NameOrTitle { get; set; } = string.Empty;
    public string NationalIdentifier { get; set; } = string.Empty;
    public string? TaxpayerId { get; set; }
    public string? Mobile { get; set; }
}
