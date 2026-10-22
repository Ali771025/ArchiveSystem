using FluentValidation;

namespace ArchiveSystem.Application.Features.CaseLoans.Commands.DeleteCaseLoan
{
    public sealed class DeleteCaseLoanCommandValidator : AbstractValidator<DeleteCaseLoanCommand>
    {
        public DeleteCaseLoanCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
