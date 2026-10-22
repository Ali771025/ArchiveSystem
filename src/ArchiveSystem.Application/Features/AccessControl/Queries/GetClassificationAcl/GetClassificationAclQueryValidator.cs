using FluentValidation;

namespace ArchiveSystem.Application.Features.AccessControl.Queries.GetClassificationAcl
{
    public sealed class GetClassificationAclQueryValidator : AbstractValidator<GetClassificationAclQuery>
    {
        public GetClassificationAclQueryValidator()
        {
            RuleFor(x => x.ClassificationId)
                .GreaterThan(0)
                .WithMessage("ClassificationId must be greater than 0.");
        }
    }
}
