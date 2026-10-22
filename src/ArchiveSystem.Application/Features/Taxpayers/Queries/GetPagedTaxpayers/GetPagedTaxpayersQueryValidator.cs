using FluentValidation;

namespace ArchiveSystem.Application.Features.Taxpayers.Queries.GetPagedTaxpayers
{
    public sealed class GetPagedTaxpayersQueryValidator : AbstractValidator<GetPagedTaxpayersQuery>
    {
        public GetPagedTaxpayersQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100);

            RuleFor(x => x.SearchTerm)
                .MaximumLength(200);

            RuleFor(x => x.FirstName)
                .MaximumLength(100);

            RuleFor(x => x.LastName)
                .MaximumLength(100);

            RuleFor(x => x.NationalId)
                .MaximumLength(10);
        }
    }
}
