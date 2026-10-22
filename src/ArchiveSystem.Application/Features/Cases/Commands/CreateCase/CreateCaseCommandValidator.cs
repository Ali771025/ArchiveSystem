using ArchiveSystem.Application.Features.Cases.Commands;
using FluentValidation;

namespace ArchiveSystem.Application.Features.Cases.Commands.CreateCase
{
    public sealed class CreateCaseCommandValidator : AbstractValidator<CreateCaseCommand>
    {
        public CreateCaseCommandValidator()
        {
            RuleFor(x => x.ExternalCaseNumber)
                .MaximumLength(100)
                .Must(CaseNumberValidation.IsValid)
                .When(x => !string.IsNullOrWhiteSpace(x.ExternalCaseNumber))
                .WithMessage(CaseNumberValidation.InvalidMessage);

            RuleFor(x => x.Address)
                .NotEmpty()
                .MaximumLength(500)
                .WithMessage("Address cannot be empty and must be at most 500 characters.");

            RuleFor(x => x.PostalCode)
                .Matches("^[0-9]{10}$")
                .When(x => !string.IsNullOrWhiteSpace(x.PostalCode))
                .WithMessage("Postal code must be exactly 10 digits.");

            RuleFor(x => x.ClassificationId)
                .GreaterThan(0)
                .WithMessage("Classification ID must be greater than 0.");

            RuleForEach(x => x.Owners).ChildRules(owner =>
            {
                owner.RuleFor(x => x.FullName)
                    .NotEmpty()
                    .MaximumLength(200);

                owner.RuleFor(x => x.NationalCode)
                    .Matches("^[0-9]{10}$")
                    .When(x => !string.IsNullOrWhiteSpace(x.NationalCode))
                    .WithMessage("National code must be 10 digits.");

                owner.RuleFor(x => x.TaxpayerId)
                    .Matches("^[0-9]{11}$|^[0-9]{14}$")
                    .When(x => !string.IsNullOrWhiteSpace(x.TaxpayerId))
                    .WithMessage("TaxpayerId must be 11 or 14 digits.");

                owner.RuleFor(x => x.Phone)
                    .Matches("^[0-9+\\-() ]{7,20}$")
                    .When(x => !string.IsNullOrWhiteSpace(x.Phone))
                    .WithMessage("Phone is not in a valid format.");
            });
        }
    }
}
