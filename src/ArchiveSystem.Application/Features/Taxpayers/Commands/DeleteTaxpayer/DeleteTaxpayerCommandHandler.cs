using ArchiveSystem.Application.Features.Taxpayers.Interfaces;
using MediatR;

namespace ArchiveSystem.Application.Features.Taxpayers.Commands.DeleteTaxpayer
{
    public sealed class DeleteTaxpayerCommandHandler : IRequestHandler<DeleteTaxpayerCommand, bool>
    {
        private readonly ITaxpayerRepository _taxpayerRepository;

        public DeleteTaxpayerCommandHandler(ITaxpayerRepository taxpayerRepository)
        {
            _taxpayerRepository = taxpayerRepository;
        }

        public async Task<bool> Handle(DeleteTaxpayerCommand request, CancellationToken cancellationToken)
        {
            var taxpayer = await _taxpayerRepository.GetByIdAsync(request.Id, cancellationToken);
            if (taxpayer is null)
            {
                return false;
            }

            taxpayer.SoftDelete();
            await _taxpayerRepository.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
