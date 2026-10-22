using ArchiveSystem.Application.Features.Taxpayers.DTOs;
using MediatR;

namespace ArchiveSystem.Application.Features.Taxpayers.Queries.GetTaxpayerById
{
    public sealed record GetTaxpayerByIdQuery(Guid Id) : IRequest<TaxpayerDetailsDto?>;
}
