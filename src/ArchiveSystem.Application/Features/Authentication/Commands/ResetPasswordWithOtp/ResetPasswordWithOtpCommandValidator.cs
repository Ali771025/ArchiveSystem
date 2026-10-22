using FluentValidation;

namespace ArchiveSystem.Application.Features.Authentication.Commands.ResetPasswordWithOtp
{
    public sealed class ResetPasswordWithOtpCommandValidator : AbstractValidator<ResetPasswordWithOtpCommand>
    {
        private const string IranianMobileRegex = "^(?:\\+98|0)?9\\d{9}$";

        public ResetPasswordWithOtpCommandValidator()
        {
            RuleFor(x => x.MobileNumber)
                .NotEmpty()
                .Matches(IranianMobileRegex)
                .WithMessage("Mobile number format is invalid. Expected an Iranian mobile number.");

            RuleFor(x => x.OtpCode)
                .NotEmpty()
                .Matches("^\\d{5}$")
                .WithMessage("کد تایید باید ۵ رقم باشد.");

            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .MinimumLength(12)
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches("[0-9]").WithMessage("Password must contain at least one number.")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
        }
    }
}
