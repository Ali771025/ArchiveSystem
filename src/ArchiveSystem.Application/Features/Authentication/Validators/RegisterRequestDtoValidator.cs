using ArchiveSystem.Application.Features.Authentication.DTOs;
using FluentValidation;

namespace ArchiveSystem.Application.Features.Authentication.Validators
{
    public sealed class RegisterRequestDtoValidator : AbstractValidator<RegisterRequestDto>
    {
        public RegisterRequestDtoValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(64);

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8);

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password)
                .WithMessage("Password and confirmation password must match.");
        }
    }
}
