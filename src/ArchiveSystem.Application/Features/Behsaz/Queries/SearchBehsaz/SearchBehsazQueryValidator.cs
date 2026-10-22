using ArchiveSystem.Domain.Enums;
using FluentValidation;

namespace ArchiveSystem.Application.Features.Behsaz.Queries.SearchBehsaz
{
    public sealed class SearchBehsazQueryValidator : AbstractValidator<SearchBehsazQuery>
    {
        public SearchBehsazQueryValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 200);

            RuleFor(x => x.TaxpayerGroup)
                .Must(group => group is null || Enum.IsDefined(typeof(TaxpayerGroup), group.Value))
                .WithMessage("TaxpayerGroup must be 1 (Real) or 2 (Legal).");

            RuleFor(x => x.NationalCode)
                .MaximumLength(20);

            RuleFor(x => x.NationalCompanyId)
                .MaximumLength(20);
        }
    }
}
