using FluentValidation;

namespace ArchiveSystem.Application.Features.AccessControl.Commands.SetUserRoles
{
    public sealed class SetUserRolesCommandValidator : AbstractValidator<SetUserRolesCommand>
    {
        public SetUserRolesCommandValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0)
                .WithMessage("UserId must be greater than 0.");

            RuleFor(x => x.RoleIds)
                .NotNull()
                .WithMessage("RoleIds is required.");

            RuleForEach(x => x.RoleIds)
                .GreaterThan(0)
                .WithMessage("RoleId must be greater than 0.");

            RuleFor(x => x.RoleIds)
                .Must(ids => ids.Distinct().Count() == ids.Count)
                .When(x => x.RoleIds is not null)
                .WithMessage("RoleIds cannot contain duplicates.");
        }
    }
}
