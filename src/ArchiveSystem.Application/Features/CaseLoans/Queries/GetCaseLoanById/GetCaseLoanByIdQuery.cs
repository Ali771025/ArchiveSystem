using ArchiveSystem.Application.Features.CaseLoans.DTOs;
using MediatR;

namespace ArchiveSystem.Application.Features.CaseLoans.Queries.GetCaseLoanById
{
    public sealed record GetCaseLoanByIdQuery(Guid Id) : IRequest<CaseLoanDetailsDto?>;
}
