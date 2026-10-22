using ArchiveSystem.Application.Features.Cases.Commands;
using FluentValidation;

namespace ArchiveSystem.Application.Features.Cases.Commands.UpdateCase
{
    public sealed class UpdateCaseCommandValidator : AbstractValidator<UpdateCaseCommand>
    {
        public UpdateCaseCommandValidator()
        {
            RuleFor(x => x.CaseId)
                .GreaterThan(0);

            RuleFor(x => x.ExternalCaseNumber)
                .MaximumLength(100)
                .Must(CaseNumberValidation.IsValid)
                .When(x => !string.IsNullOrWhiteSpace(x.ExternalCaseNumber))
                .WithMessage(CaseNumberValidation.InvalidMessage);

            RuleFor(x => x.Address)
                .NotEmpty()
                .MaximumLength(500);

            RuleFor(x => x.PostalCode)
                .Matches("^[0-9]{10}$")
                .When(x => !string.IsNullOrWhiteSpace(x.PostalCode));

            RuleForEach(x => x.Owners).ChildRules(owner =>
            {
                owner.RuleFor(x => x.FullName)
                    .NotEmpty()
                    .MaximumLength(200);

                owner.RuleFor(x => x.NationalCode)
                    .Matches("^[0-9]{10}$")
                    .When(x => !string.IsNullOrWhiteSpace(x.NationalCode));

                owner.RuleFor(x => x.TaxpayerId)
                    .Matches("^[0-9]{11}$|^[0-9]{14}$")
                    .When(x => !string.IsNullOrWhiteSpace(x.TaxpayerId));

                owner.RuleFor(x => x.Phone)
                    .Matches("^[0-9+\\-() ]{7,20}$")
                    .When(x => !string.IsNullOrWhiteSpace(x.Phone));
            });
        }
    }
}
