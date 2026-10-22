using ArchiveSystem.Application.Features.CaseLoans.DTOs;
using ArchiveSystem.Application.Features.UserManagement.Common;
using MediatR;

namespace ArchiveSystem.Application.Features.CaseLoans.Queries.GetPagedCaseLoans
{
    public sealed record GetPagedCaseLoansQuery(
        int PageNumber = 1,
        int PageSize = 10,
        string? CaseNumber = null,
        string? NationalId = null,
        string? BorrowerLastName = null,
        string? ColumnFilterField = null,
        string? ColumnFilterOperator = null,
        string? ColumnFilterValue = null) : IRequest<PagedResponse<CaseLoanListItemDto>>;
}
