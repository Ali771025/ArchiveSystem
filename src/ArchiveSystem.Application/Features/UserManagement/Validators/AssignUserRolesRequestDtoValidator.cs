using ArchiveSystem.Application.Features.UserManagement.DTOs;
using FluentValidation;

namespace ArchiveSystem.Application.Features.UserManagement.Validators
{
    public sealed class AssignUserRolesRequestDtoValidator : AbstractValidator<AssignUserRolesRequestDto>
    {
        public AssignUserRolesRequestDtoValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0);

            RuleFor(x => x.Roles)
                .NotNull();

            RuleForEach(x => x.Roles)
                .NotEmpty()
                .MaximumLength(128);

            RuleFor(x => x.Roles)
                .Must(roles => roles.Distinct(StringComparer.OrdinalIgnoreCase).Count() == roles.Count)
                .When(x => x.Roles is not null)
                .WithMessage("Roles cannot contain duplicates.");
        }
    }
}
