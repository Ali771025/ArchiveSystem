using FluentValidation;

namespace ArchiveSystem.Application.Features.AccessControl.Queries.GetRolePermissionIds
{
    public sealed class GetRolePermissionIdsQueryValidator : AbstractValidator<GetRolePermissionIdsQuery>
    {
        public GetRolePermissionIdsQueryValidator()
        {
            RuleFor(x => x.RoleId)
                .GreaterThan(0)
                .WithMessage("RoleId must be greater than 0.");
        }
    }
}
