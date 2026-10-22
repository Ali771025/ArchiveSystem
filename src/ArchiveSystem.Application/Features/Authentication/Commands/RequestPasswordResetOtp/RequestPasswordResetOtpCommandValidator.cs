using FluentValidation;

namespace ArchiveSystem.Application.Features.Authentication.Commands.RequestPasswordResetOtp
{
    public sealed class RequestPasswordResetOtpCommandValidator : AbstractValidator<RequestPasswordResetOtpCommand>
    {
        private const string IranianMobileRegex = "^(?:\\+98|0)?9\\d{9}$";

        public RequestPasswordResetOtpCommandValidator()
        {
            RuleFor(x => x.MobileNumber)
                .NotEmpty()
                .Matches(IranianMobileRegex)
                .WithMessage("Mobile number format is invalid. Expected an Iranian mobile format.");

            RuleFor(x => x.CorrelationId)
                .NotEmpty()
                .MaximumLength(100);
        }
    }
}
