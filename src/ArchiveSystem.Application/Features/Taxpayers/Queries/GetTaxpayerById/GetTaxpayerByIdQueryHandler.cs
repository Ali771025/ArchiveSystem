using ArchiveSystem.Application.Features.Taxpayers.DTOs;
using ArchiveSystem.Application.Features.Taxpayers.Interfaces;
using MediatR;

namespace ArchiveSystem.Application.Features.Taxpayers.Queries.GetTaxpayerById
{
    public sealed class GetTaxpayerByIdQueryHandler : IRequestHandler<GetTaxpayerByIdQuery, TaxpayerDetailsDto?>
    {
        private readonly ITaxpayerRepository _taxpayerRepository;

        public GetTaxpayerByIdQueryHandler(ITaxpayerRepository taxpayerRepository)
        {
            _taxpayerRepository = taxpayerRepository;
        }

        public Task<TaxpayerDetailsDto?> Handle(GetTaxpayerByIdQuery request, CancellationToken cancellationToken)
        {
            return _taxpayerRepository.GetDetailsByIdAsync(request.Id, cancellationToken);
        }
    }
}
