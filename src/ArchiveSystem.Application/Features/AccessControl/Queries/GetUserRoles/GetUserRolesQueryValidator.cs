using FluentValidation;

namespace ArchiveSystem.Application.Features.AccessControl.Queries.GetUserRoles
{
    public sealed class GetUserRolesQueryValidator : AbstractValidator<GetUserRolesQuery>
    {
        public GetUserRolesQueryValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0)
                .WithMessage("UserId must be greater than 0.");
        }
    }
}
