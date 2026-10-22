using ArchiveSystem.Application.Features.CaseLoans.DTOs;
using ArchiveSystem.Application.Features.CaseLoans.Interfaces;
using ArchiveSystem.Application.Features.UserManagement.Common;
using MediatR;

namespace ArchiveSystem.Application.Features.CaseLoans.Queries.GetPagedCaseLoans
{
    public sealed class GetPagedCaseLoansQueryHandler : IRequestHandler<GetPagedCaseLoansQuery, PagedResponse<CaseLoanListItemDto>>
    {
        private readonly ICaseLoanRepository _caseLoanRepository;

        public GetPagedCaseLoansQueryHandler(ICaseLoanRepository caseLoanRepository)
        {
            _caseLoanRepository = caseLoanRepository;
        }

        public Task<PagedResponse<CaseLoanListItemDto>> Handle(GetPagedCaseLoansQuery request, CancellationToken cancellationToken)
        {
            return _caseLoanRepository.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.CaseNumber,
                request.NationalId,
                request.BorrowerLastName,
                request.ColumnFilterField,
                request.ColumnFilterOperator,
                request.ColumnFilterValue,
                cancellationToken);
        }
    }
}
