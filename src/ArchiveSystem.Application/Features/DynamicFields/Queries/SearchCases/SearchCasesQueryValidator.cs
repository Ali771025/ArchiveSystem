using FluentValidation;

namespace ArchiveSystem.Application.Features.DynamicFields.Queries.SearchCases
{
    public sealed class SearchCasesQueryValidator : AbstractValidator<SearchCasesQuery>
    {
        public SearchCasesQueryValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThan(0);

            RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .LessThanOrEqualTo(200);
        }
    }
}
