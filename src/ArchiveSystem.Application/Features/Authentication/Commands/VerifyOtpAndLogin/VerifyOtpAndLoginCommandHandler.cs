using ArchiveSystem.Application.Common.Interfaces.Security;
using ArchiveSystem.Application.Features.Authentication.Otp;
using MediatR;

namespace ArchiveSystem.Application.Features.Authentication.Commands.VerifyOtpAndLogin
{
    public sealed class VerifyOtpAndLoginCommandHandler : IRequestHandler<VerifyOtpAndLoginCommand, VerifyOtpAndLoginResult>
    {
        private const string InvalidOtpMessage = "Invalid mobile number or OTP code.";
        private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);

        private readonly ILoginOtpUserService _loginOtpUserService;
        private readonly IIdentityService _identityService;
        private readonly IRefreshTokenIssuer _refreshTokenIssuer;

        public VerifyOtpAndLoginCommandHandler(
            ILoginOtpUserService loginOtpUserService,
            IIdentityService identityService,
            IRefreshTokenIssuer refreshTokenIssuer)
        {
            _loginOtpUserService = loginOtpUserService;
            _identityService = identityService;
            _refreshTokenIssuer = refreshTokenIssuer;
        }

        public async Task<VerifyOtpAndLoginResult> Handle(VerifyOtpAndLoginCommand request, CancellationToken cancellationToken)
        {
            var normalizedMobile = NormalizeIranianMobile(request.MobileNumber);
            var user = await _loginOtpUserService.FindActiveByMobileAsync(normalizedMobile, cancellationToken);
            if (user is null)
            {
                return new VerifyOtpAndLoginResult(false, InvalidOtpMessage);
            }

            var isValid = await _loginOtpUserService.VerifyLoginOtpAsync(user.UserId, request.OtpCode, cancellationToken);
            if (!isValid)
            {
                return new VerifyOtpAndLoginResult(false, InvalidOtpMessage);
            }

            var identityUser = await _identityService.FindByIdAsync(user.UserId, cancellationToken);
            if (identityUser is null)
            {
                return new VerifyOtpAndLoginResult(false, InvalidOtpMessage);
            }

            var accessToken = await _identityService.CreateAccessTokenAsync(identityUser, cancellationToken);
            var refreshToken = await _refreshTokenIssuer.IssueAsync(user.UserId, cancellationToken);
            var expiresAtUtc = DateTimeOffset.UtcNow.Add(AccessTokenLifetime);

            return new VerifyOtpAndLoginResult(
                Succeeded: true,
                Message: "Login completed successfully.",
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                ExpiresAtUtc: expiresAtUtc);
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
