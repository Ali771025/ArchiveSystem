using ArchiveSystem.Application.Features.Authentication.DTOs;
using FluentValidation;

namespace ArchiveSystem.Application.Features.Authentication.Validators
{
    public sealed class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
    {
        public LoginRequestDtoValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty()
                .MaximumLength(64);

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8);
        }
    }
}
