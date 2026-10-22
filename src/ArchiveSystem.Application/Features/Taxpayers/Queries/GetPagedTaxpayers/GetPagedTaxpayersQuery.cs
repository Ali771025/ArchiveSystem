using ArchiveSystem.Application.Features.Taxpayers.DTOs;
using ArchiveSystem.Application.Features.UserManagement.Common;
using MediatR;

namespace ArchiveSystem.Application.Features.Taxpayers.Queries.GetPagedTaxpayers
{
    public sealed record GetPagedTaxpayersQuery(
        int PageNumber = 1,
        int PageSize = 10,
        string? SearchTerm = null,
        string? FirstName = null,
        string? LastName = null,
        string? NationalId = null,
        string? ColumnFilterField = null,
        string? ColumnFilterOperator = null,
        string? ColumnFilterValue = null) : IRequest<PagedResponse<TaxpayerListItemDto>>;
}
