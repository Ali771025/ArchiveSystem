using ArchiveSystem.Application.Features.Authentication.Otp;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ArchiveSystem.Application.Features.Authentication.Commands.RequestOtp
{
    public sealed class RequestOtpCommandHandler : IRequestHandler<RequestOtpCommand, RequestOtpResult>
    {
        private const string NeutralMessage = "If the mobile number is registered, an OTP has been sent.";

        private readonly IPhoneOtpIdentityService _phoneOtpIdentityService;
        private readonly ISmsSender _smsSender;
        private readonly IAuthSlidingWindowRateLimiter _rateLimiter;
        private readonly ILogger<RequestOtpCommandHandler> _logger;

        public RequestOtpCommandHandler(
            IPhoneOtpIdentityService phoneOtpIdentityService,
            ISmsSender smsSender,
            IAuthSlidingWindowRateLimiter rateLimiter,
            ILogger<RequestOtpCommandHandler> logger)
        {
            _phoneOtpIdentityService = phoneOtpIdentityService;
            _smsSender = smsSender;
            _rateLimiter = rateLimiter;
            _logger = logger;
        }

        public async Task<RequestOtpResult> Handle(RequestOtpCommand request, CancellationToken cancellationToken)
        {
            var normalizedMobile = NormalizeIranianMobile(request.MobileNumber);

            var rateLimitDecision = await _rateLimiter.EvaluateAndConsumeAsync(
                dimension: "otp-mobile",
                identifier: normalizedMobile,
                limit: 1,
                window: TimeSpan.FromMinutes(2),
                cancellationToken: cancellationToken);

            if (!rateLimitDecision.Allowed)
            {
                return new RequestOtpResult(
                    Succeeded: true,
                    Message: NeutralMessage,
                    NextAllowedAtUtc: rateLimitDecision.WindowEndsAtUtc);
            }

            var user = await _phoneOtpIdentityService.FindActiveByMobileAsync(normalizedMobile, cancellationToken);
            if (user is null)
            {
                _logger.LogInformation("OTP request processed for unknown mobile {Mobile}.", MaskMobile(normalizedMobile));
                return new RequestOtpResult(Succeeded: true, Message: NeutralMessage);
            }

            try
            {
                var otpCode = await _phoneOtpIdentityService.GeneratePhoneOtpAsync(user.UserId, cancellationToken);
                await _smsSender.SendSmsAsync(user.MobileNumber, otpCode, cancellationToken);
                _logger.LogInformation("OTP sent successfully for user {UserId} mobile {Mobile}.", user.UserId, MaskMobile(user.MobileNumber));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OTP request accepted but SMS dispatch failed for user {UserId}.", user.UserId);
            }

            return new RequestOtpResult(Succeeded: true, Message: NeutralMessage);
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

        private static string MaskMobile(string mobile)
        {
            if (mobile.Length < 7)
            {
                return "***";
            }

            return $"{mobile[..4]}****{mobile[^3..]}";
        }
    }
}
