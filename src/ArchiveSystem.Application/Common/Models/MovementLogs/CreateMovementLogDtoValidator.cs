using FluentValidation;

namespace ArchiveSystem.Application.Common.Models.MovementLogs
{
    public sealed class CreateMovementLogDtoValidator : AbstractValidator<CreateMovementLogDto>
    {
        public CreateMovementLogDtoValidator()
        {
            RuleFor(x => x.DossierNumber)
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(x => x.ReceiverFirstName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.ReceiverLastName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.NationalCode)
                .NotEmpty()
                .Length(10)
                .Matches(@"^\d{10}$");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .MaximumLength(15);

            RuleFor(x => x.CheckoutDate)
                .Must(x => x != default)
                .WithMessage("Checkout date is required.");

            RuleFor(x => x.ExpectedReturnDate)
                .GreaterThanOrEqualTo(x => x.CheckoutDate)
                .When(x => x.ExpectedReturnDate.HasValue);

            RuleFor(x => x.Description)
                .MaximumLength(500);
        }
    }
}
