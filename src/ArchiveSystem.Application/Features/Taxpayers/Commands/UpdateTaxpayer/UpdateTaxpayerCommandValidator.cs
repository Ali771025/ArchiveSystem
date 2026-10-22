using ArchiveSystem.Domain.Enums;
using FluentValidation;

namespace ArchiveSystem.Application.Features.Taxpayers.Commands.UpdateTaxpayer
{
    public sealed class UpdateTaxpayerCommandValidator : AbstractValidator<UpdateTaxpayerCommand>
    {
        public UpdateTaxpayerCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty();

            RuleFor(x => x.TaxpayerType)
                .IsInEnum();

            RuleFor(x => x.FirstName)
                .NotEmpty()
                .When(x => x.TaxpayerType == TaxpayerType.NaturalPerson)
                .WithMessage("نام برای مودی حقیقی الزامی است.")
                .MaximumLength(100);

            RuleFor(x => x.LastName)
                .NotEmpty()
                .When(x => x.TaxpayerType == TaxpayerType.NaturalPerson)
                .WithMessage("نام خانوادگی برای مودی حقیقی الزامی است.")
                .MaximumLength(100);

            RuleFor(x => x.NationalId)
                .NotEmpty()
                .When(x => x.TaxpayerType == TaxpayerType.NaturalPerson)
                .WithMessage("کد ملی برای مودی حقیقی الزامی است.")
                .Matches("^[0-9]{10}$")
                .When(x => x.TaxpayerType == TaxpayerType.NaturalPerson && !string.IsNullOrWhiteSpace(x.NationalId))
                .WithMessage("NationalId must be exactly 10 digits.");

            RuleFor(x => x.CompanyName)
                .NotEmpty()
                .When(x => x.TaxpayerType == TaxpayerType.LegalEntity)
                .WithMessage("نام شرکت/سازمان برای مودی حقوقی الزامی است.")
                .MaximumLength(100);

            RuleFor(x => x.NationalCompanyId)
                .NotEmpty()
                .When(x => x.TaxpayerType == TaxpayerType.LegalEntity)
                .WithMessage("شناسه ملی برای مودی حقوقی الزامی است.")
                .Matches("^[0-9]{11}$")
                .When(x => x.TaxpayerType == TaxpayerType.LegalEntity && !string.IsNullOrWhiteSpace(x.NationalCompanyId))
                .WithMessage("شناسه ملی باید دقیقاً ۱۱ رقم باشد.");

            RuleFor(x => x.IdentityNumber)
                .MaximumLength(50);

            RuleFor(x => x.Mobile)
                .Matches("^09[0-9]{9}$")
                .When(x => !string.IsNullOrWhiteSpace(x.Mobile))
                .WithMessage("شماره همراه باید با 09 شروع شود و دقیقاً 11 رقم باشد.");

            RuleFor(x => x.LandlinePhone)
                .MaximumLength(20);

            RuleFor(x => x.PostalCode)
                .MaximumLength(10)
                .Matches("^[0-9]{10}$")
                .When(x => !string.IsNullOrWhiteSpace(x.PostalCode))
                .WithMessage("PostalCode must be exactly 10 digits.");

            RuleFor(x => x.Address)
                .MaximumLength(1000);
        }
    }
}
