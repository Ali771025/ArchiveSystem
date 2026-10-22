using ArchiveSystem.Application.Features.Taxpayers.DTOs;
using ArchiveSystem.Application.Features.Taxpayers.Interfaces;
using ArchiveSystem.Application.Features.UserManagement.Common;
using MediatR;

namespace ArchiveSystem.Application.Features.Taxpayers.Queries.GetPagedTaxpayers
{
    public sealed class GetPagedTaxpayersQueryHandler : IRequestHandler<GetPagedTaxpayersQuery, PagedResponse<TaxpayerListItemDto>>
    {
        private readonly ITaxpayerRepository _taxpayerRepository;

        public GetPagedTaxpayersQueryHandler(ITaxpayerRepository taxpayerRepository)
        {
            _taxpayerRepository = taxpayerRepository;
        }

        public Task<PagedResponse<TaxpayerListItemDto>> Handle(GetPagedTaxpayersQuery request, CancellationToken cancellationToken)
        {
            return _taxpayerRepository.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.FirstName,
                request.LastName,
                request.NationalId,
                request.ColumnFilterField,
                request.ColumnFilterOperator,
                request.ColumnFilterValue,
                cancellationToken);
        }
    }
}
