using FluentValidation;

namespace ArchiveSystem.Application.Common.Models.AdvancedSearch
{
    public sealed class AdvancedSearchQueryDtoValidator : AbstractValidator<AdvancedSearchQueryDto>
    {
        public AdvancedSearchQueryDtoValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0);

            RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .LessThanOrEqualTo(100);

            RuleFor(x => x.DossierNumber)
                .MaximumLength(64);

            RuleFor(x => x.PostalCode)
                .MaximumLength(32);

            RuleFor(x => x.OwnerFirstName)
                .MaximumLength(128);

            RuleFor(x => x.OwnerLastName)
                .MaximumLength(128);

            RuleFor(x => x.CreatedBy)
                .MaximumLength(200);

            RuleFor(x => x)
                .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate.Value.Date <= x.ToDate.Value.Date)
                .WithMessage("FromDate must be less than or equal to ToDate.");

            RuleForEach(x => x.Tags!)
                .NotEmpty()
                .MaximumLength(128)
                .When(x => x.Tags is { Count: > 0 });

            RuleFor(x => x.SortBy).IsInEnum();
            RuleFor(x => x.SortDirection).IsInEnum();
        }
    }
}
