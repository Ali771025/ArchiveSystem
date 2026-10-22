using MediatR;

namespace ArchiveSystem.Application.Features.Taxpayers.Commands.DeleteTaxpayer
{
    public sealed record DeleteTaxpayerCommand(Guid Id) : IRequest<bool>;
}
