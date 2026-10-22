using ArchiveSystem.Application.Features.Authentication.Otp;
using ArchiveSystem.Application.Features.UserManagement.Interfaces;
using MediatR;

namespace ArchiveSystem.Application.Features.Authentication.Commands.ResetPasswordWithOtp
{
    public sealed class ResetPasswordWithOtpCommandHandler : IRequestHandler<ResetPasswordWithOtpCommand, ResetPasswordWithOtpResult>
    {
        private const string InvalidOtpMessage = "کد تایید یا شماره موبایل معتبر نیست.";

        private readonly ILoginOtpUserService _otpUserService;
        private readonly IUserManagementService _userManagementService;

        public ResetPasswordWithOtpCommandHandler(
            ILoginOtpUserService otpUserService,
            IUserManagementService userManagementService)
        {
            _otpUserService = otpUserService;
            _userManagementService = userManagementService;
        }

        public async Task<ResetPasswordWithOtpResult> Handle(ResetPasswordWithOtpCommand request, CancellationToken cancellationToken)
        {
            var normalizedMobile = NormalizeIranianMobile(request.MobileNumber);
            var user = await _otpUserService.FindActiveByMobileAsync(normalizedMobile, cancellationToken);
            if (user is null)
            {
                return new ResetPasswordWithOtpResult(false, InvalidOtpMessage);
            }

            var isValid = await _otpUserService.VerifyPasswordResetOtpAsync(user.UserId, request.OtpCode, cancellationToken);
            if (!isValid)
            {
                return new ResetPasswordWithOtpResult(false, InvalidOtpMessage);
            }

            var resetSucceeded = await _userManagementService.ResetPasswordAsync(
                user.UserId,
                request.NewPassword,
                "password-reset-otp",
                cancellationToken);

            return resetSucceeded
                ? new ResetPasswordWithOtpResult(true, "گذرواژه با موفقیت تغییر کرد.")
                : new ResetPasswordWithOtpResult(false, InvalidOtpMessage);
        }

        private static string NormalizeIranianMobile(string input)
        {
            var compact = input.Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .Trim();

            if (compact.StartsWith("+98", StringComparison.Ordinal))
            {
                return "0" + compact[3..];
            }

            if (compact.StartsWith("98", StringComparison.Ordinal) && compact.Length == 12)
            {
                return "0" + compact[2..];
            }

            return compact;
        }
    }
}
