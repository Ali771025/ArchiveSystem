using FluentValidation;

namespace ArchiveSystem.Application.Features.CaseLoans.Queries.GetPagedCaseLoans
{
    public sealed class GetPagedCaseLoansQueryValidator : AbstractValidator<GetPagedCaseLoansQuery>
    {
        public GetPagedCaseLoansQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100);

            RuleFor(x => x.CaseNumber)
                .MaximumLength(50);

            RuleFor(x => x.NationalId)
                .MaximumLength(10);

            RuleFor(x => x.BorrowerLastName)
                .MaximumLength(100);
        }
    }
}
