using FluentValidation;

namespace ArchiveSystem.Application.Features.AccessControl.Queries.GetTagAcl
{
    public sealed class GetTagAclQueryValidator : AbstractValidator<GetTagAclQuery>
    {
        public GetTagAclQueryValidator()
        {
            RuleFor(x => x.TagId)
                .GreaterThan(0)
                .WithMessage("TagId must be greater than 0.");
        }
    }
}
