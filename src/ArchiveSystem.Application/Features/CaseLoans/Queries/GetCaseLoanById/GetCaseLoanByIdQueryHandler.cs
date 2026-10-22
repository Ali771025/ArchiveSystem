using ArchiveSystem.Application.Features.CaseLoans.DTOs;
using ArchiveSystem.Application.Features.CaseLoans.Interfaces;
using MediatR;

namespace ArchiveSystem.Application.Features.CaseLoans.Queries.GetCaseLoanById
{
    public sealed class GetCaseLoanByIdQueryHandler : IRequestHandler<GetCaseLoanByIdQuery, CaseLoanDetailsDto?>
    {
        private readonly ICaseLoanRepository _caseLoanRepository;

        public GetCaseLoanByIdQueryHandler(ICaseLoanRepository caseLoanRepository)
        {
            _caseLoanRepository = caseLoanRepository;
        }

        public Task<CaseLoanDetailsDto?> Handle(GetCaseLoanByIdQuery request, CancellationToken cancellationToken)
        {
            return _caseLoanRepository.GetDetailsByIdAsync(request.Id, cancellationToken);
        }
    }
}
