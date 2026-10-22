using FluentValidation;

namespace ArchiveSystem.Application.Features.Authentication.Commands.RequestOtp
{
    public sealed class RequestOtpCommandValidator : AbstractValidator<RequestOtpCommand>
    {
        private const string IranianMobileRegex = "^(?:\\+98|0)?9\\d{9}$";

        public RequestOtpCommandValidator()
        {
            RuleFor(x => x.MobileNumber)
                .NotEmpty()
                .Matches(IranianMobileRegex)
                .WithMessage("Mobile number format is invalid. Expected an Iranian mobile number.");
        }
    }
}
