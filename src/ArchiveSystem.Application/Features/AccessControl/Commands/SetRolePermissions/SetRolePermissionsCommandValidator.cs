using ArchiveSystem.Application.Common.Interfaces.Repositories;
using FluentValidation;

namespace ArchiveSystem.Application.Features.AccessControl.Commands.SetRolePermissions
{
    public sealed class SetRolePermissionsCommandValidator : AbstractValidator<SetRolePermissionsCommand>
    {
        public SetRolePermissionsCommandValidator(IPermissionRepository permissionRepository)
        {
            RuleFor(x => x.RoleId)
                .GreaterThan(0)
                .WithMessage("RoleId must be greater than 0.");

            RuleFor(x => x.PermissionIds)
                .NotNull()
                .WithMessage("PermissionIds is required.");

            RuleForEach(x => x.PermissionIds)
                .GreaterThan(0)
                .WithMessage("PermissionId must be greater than 0.");

            RuleFor(x => x.PermissionIds)
                .Must(ids => ids.Distinct().Count() == ids.Count)
                .When(x => x.PermissionIds is not null)
                .WithMessage("PermissionIds cannot contain duplicates.");

            RuleFor(x => x.PermissionIds)
                .MustAsync(async (ids, cancellationToken) =>
                {
                    if (ids is null || ids.Count == 0)
                    {
                        return true;
                    }

                    var permissions = await permissionRepository.GetAllAsync(cancellationToken);
                    var knownPermissionIds = permissions.Select(x => x.Id).ToHashSet();
                    return ids.All(knownPermissionIds.Contains);
                })
                .WithMessage("One or more permissions were not found.");
        }
    }
}
