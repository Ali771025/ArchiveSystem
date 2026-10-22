using FluentValidation;

namespace ArchiveSystem.Application.Features.CaseLoans.Commands.UpdateCaseLoan
{
    public sealed class UpdateCaseLoanCommandValidator : AbstractValidator<UpdateCaseLoanCommand>
    {
        public UpdateCaseLoanCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();

            RuleFor(x => x.BorrowerFirstName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.BorrowerLastName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.CaseNumber)
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(x => x.NationalId)
                .NotEmpty()
                .Matches("^[0-9]{10}$")
                .WithMessage("NationalId must be exactly 10 digits.");

            RuleFor(x => x.ContactNumber)
                .NotEmpty()
                .MaximumLength(20);

            RuleFor(x => x.CheckoutDate)
                .NotEmpty();

            RuleFor(x => x.ExpectedReturnDate)
                .GreaterThanOrEqualTo(x => x.CheckoutDate)
                .When(x => x.ExpectedReturnDate.HasValue)
                .WithMessage("ExpectedReturnDate must be on or after CheckoutDate.");

            RuleFor(x => x.ActualReturnDate)
                .GreaterThanOrEqualTo(x => x.CheckoutDate)
                .When(x => x.ActualReturnDate.HasValue)
                .WithMessage("ActualReturnDate must be on or after CheckoutDate.");

            RuleFor(x => x.ActualReturnDate)
                .NotNull()
                .When(x => x.Status == "Returned" || x.IsReturned)
                .WithMessage("تاریخ واقعی بازگشت برای وضعیت برگشت داده شده الزامی است.");

            RuleFor(x => x.Status)
                .Must(status => string.IsNullOrWhiteSpace(status) || status is "Loaned" or "Returned" or "Overdue" or "Cancelled")
                .WithMessage("Status is invalid.");

            RuleFor(x => x.ReceiverUnit)
                .MaximumLength(150);

            RuleFor(x => x.RegisteredBy)
                .MaximumLength(150);

            RuleFor(x => x.Description)
                .MaximumLength(1000);
        }
    }
}
