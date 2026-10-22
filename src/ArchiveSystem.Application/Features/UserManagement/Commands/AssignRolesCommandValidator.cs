using FluentValidation;

namespace ArchiveSystem.Application.Features.UserManagement.Commands
{
    public sealed class AssignRolesCommandValidator : AbstractValidator<AssignRolesCommand>
    {
        public AssignRolesCommandValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0);

            RuleFor(x => x.Roles)
                .NotNull();

            RuleForEach(x => x.Roles)
                .NotEmpty()
                .MaximumLength(100);
        }
    }
}
