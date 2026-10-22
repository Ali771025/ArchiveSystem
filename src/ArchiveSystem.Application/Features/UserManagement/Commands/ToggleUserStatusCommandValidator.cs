using FluentValidation;

namespace ArchiveSystem.Application.Features.UserManagement.Commands
{
    public sealed class ToggleUserStatusCommandValidator : AbstractValidator<ToggleUserStatusCommand>
    {
        public ToggleUserStatusCommandValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0);
        }
    }
}
