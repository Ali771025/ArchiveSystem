using ArchiveSystem.Application.Features.Authentication.DTOs;
using FluentValidation;

namespace ArchiveSystem.Application.Features.Authentication.Validators
{
    public sealed class ForgotPasswordRequestDtoValidator : AbstractValidator<ForgotPasswordRequestDto>
    {
        public ForgotPasswordRequestDtoValidator()
        {
            RuleFor(x => x.UsernameOrEmail)
                .NotEmpty()
                .MaximumLength(256);
        }
    }
}
