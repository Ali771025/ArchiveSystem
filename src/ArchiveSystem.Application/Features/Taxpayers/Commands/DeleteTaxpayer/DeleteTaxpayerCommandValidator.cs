using FluentValidation;

namespace ArchiveSystem.Application.Features.Taxpayers.Commands.DeleteTaxpayer
{
    public sealed class DeleteTaxpayerCommandValidator : AbstractValidator<DeleteTaxpayerCommand>
    {
        public DeleteTaxpayerCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
