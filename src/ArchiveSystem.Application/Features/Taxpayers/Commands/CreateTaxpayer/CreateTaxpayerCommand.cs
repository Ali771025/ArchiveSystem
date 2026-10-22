using ArchiveSystem.Application.Features.Taxpayers.DTOs;
using ArchiveSystem.Domain.Enums;
using MediatR;

namespace ArchiveSystem.Application.Features.Taxpayers.Commands.CreateTaxpayer
{
    public sealed record CreateTaxpayerCommand(
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
        string? Address) : IRequest<TaxpayerDetailsDto>;
}
