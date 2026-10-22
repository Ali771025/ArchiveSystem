using ArchiveSystem.Application.Features.AccessControl.Interfaces;
using FluentValidation;

namespace ArchiveSystem.Application.Features.AccessControl.Commands.CreateRole
{
    public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
    {
        public CreateRoleCommandValidator(IIdentityRoleService identityRoleService)
        {
            RuleFor(x => x.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .MaximumLength(100)
                .MustAsync(async (name, cancellationToken) =>
                {
                    var normalizedName = name.Trim();
                    var roles = await identityRoleService.GetRolesAsync(cancellationToken);
                    return roles.All(role => !string.Equals(role.Name, normalizedName, StringComparison.OrdinalIgnoreCase));
                })
                .WithMessage("A role with the same name already exists.");
        }
    }
}
