using FluentValidation;

namespace ArchiveSystem.Application.Features.Authentication.Commands.VerifyOtpAndLogin
{
    public sealed class VerifyOtpAndLoginCommandValidator : AbstractValidator<VerifyOtpAndLoginCommand>
    {
        private const string IranianMobileRegex = "^(?:\\+98|0)?9\\d{9}$";

        public VerifyOtpAndLoginCommandValidator()
        {
            RuleFor(x => x.MobileNumber)
                .NotEmpty()
                .Matches(IranianMobileRegex)
                .WithMessage("Mobile number format is invalid. Expected an Iranian mobile number.");

            RuleFor(x => x.OtpCode)
                .NotEmpty()
                .Matches("^\\d{5}$")
                .WithMessage("کد تایید باید ۵ رقم باشد.");
        }
    }
}
