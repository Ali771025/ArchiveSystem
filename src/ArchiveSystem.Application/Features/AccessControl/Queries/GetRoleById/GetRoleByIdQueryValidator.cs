using FluentValidation;

namespace ArchiveSystem.Application.Features.AccessControl.Queries.GetRoleById
{
    public sealed class GetRoleByIdQueryValidator : AbstractValidator<GetRoleByIdQuery>
    {
        public GetRoleByIdQueryValidator()
        {
            RuleFor(x => x.RoleId)
                .GreaterThan(0)
                .WithMessage("RoleId must be greater than 0.");
        }
    }
}
