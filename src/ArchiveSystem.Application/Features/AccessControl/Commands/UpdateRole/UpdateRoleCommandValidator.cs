using ArchiveSystem.Application.Features.AccessControl.Interfaces;
using FluentValidation;

namespace ArchiveSystem.Application.Features.AccessControl.Commands.UpdateRole
{
    public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
    {
        public UpdateRoleCommandValidator(IIdentityRoleService identityRoleService)
        {
            RuleFor(x => x.RoleId)
                .GreaterThan(0)
                .WithMessage("RoleId must be greater than 0.");

            RuleFor(x => x.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .MaximumLength(100)
                .MustAsync(async (command, name, cancellationToken) =>
                {
                    var normalizedName = name.Trim();
                    var roles = await identityRoleService.GetRolesAsync(cancellationToken);
                    return roles.All(role =>
                        !string.Equals(role.Name, normalizedName, StringComparison.OrdinalIgnoreCase) ||
                        role.Id == command.RoleId);
                })
                .WithMessage("A role with the same name already exists.");
        }
    }
}
