using ArchiveSystem.Domain.Enums;
using FluentValidation;

namespace ArchiveSystem.Application.Features.Owners.Models;

public sealed class CreateOwnerDtoValidator : AbstractValidator<CreateOwnerDto>
{
    public CreateOwnerDtoValidator()
    {
        RuleFor(x => x.OwnerType).IsInEnum();

        When(x => x.OwnerType == OwnerType.Natural, () =>
        {
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.NationalCode).NotEmpty().Matches("^[0-9]{10}$");
        });

        When(x => x.OwnerType == OwnerType.Legal, () =>
        {
            RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.NationalId).NotEmpty().Matches("^[0-9]{10,11}$");
        });

        RuleFor(x => x.TaxpayerId)
            .Matches("^[0-9]{11}$|^[0-9]{14}$")
            .When(x => !string.IsNullOrWhiteSpace(x.TaxpayerId));
    }
}
