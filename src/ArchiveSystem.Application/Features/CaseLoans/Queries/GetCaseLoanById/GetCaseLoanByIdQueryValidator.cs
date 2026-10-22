using FluentValidation;

namespace ArchiveSystem.Application.Features.CaseLoans.Queries.GetCaseLoanById
{
    public sealed class GetCaseLoanByIdQueryValidator : AbstractValidator<GetCaseLoanByIdQuery>
    {
        public GetCaseLoanByIdQueryValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
