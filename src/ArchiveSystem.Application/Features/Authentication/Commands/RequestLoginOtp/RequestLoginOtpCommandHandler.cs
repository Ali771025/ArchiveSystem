using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using ArchiveSystem.Application.Features.Authentication.Otp;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ArchiveSystem.Application.Features.Authentication.Commands.RequestLoginOtp
{
    public sealed class RequestLoginOtpCommandHandler : IRequestHandler<RequestLoginOtpCommand, RequestLoginOtpResult>
    {
        private const int MaxRequestsPerWindow = 3;
        private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan MinimumResponseLatency = TimeSpan.FromMilliseconds(650);

        private readonly IAuthSlidingWindowRateLimiter _rateLimiter;
        private readonly ILoginOtpUserService _otpUserService;
        private readonly ILoginOtpOutboxService _otpOutboxService;
        private readonly ILogger<RequestLoginOtpCommandHandler> _logger;

        public RequestLoginOtpCommandHandler(
            IAuthSlidingWindowRateLimiter rateLimiter,
            ILoginOtpUserService otpUserService,
            ILoginOtpOutboxService otpOutboxService,
            ILogger<RequestLoginOtpCommandHandler> logger)
        {
            _rateLimiter = rateLimiter;
            _otpUserService = otpUserService;
            _otpOutboxService = otpOutboxService;
            _logger = logger;
        }

        public async Task<RequestLoginOtpResult> Handle(RequestLoginOtpCommand request, CancellationToken cancellationToken)
        {
            var startedAt = Stopwatch.StartNew();
            var normalizedMobile = NormalizeIranianMobile(request.MobileNumber);
            var clientIp = string.IsNullOrWhiteSpace(request.ClientIpAddress) ? "unknown" : request.ClientIpAddress.Trim();
            var maskedMobile = MaskMobile(normalizedMobile);

            var phoneDecision = await _rateLimiter.EvaluateAndConsumeAsync(
                dimension: "otp-phone",
                identifier: normalizedMobile,
                limit: MaxRequestsPerWindow,
                window: RateLimitWindow,
                cancellationToken: cancellationToken);

            var ipDecision = await _rateLimiter.EvaluateAndConsumeAsync(
                dimension: "otp-ip",
                identifier: clientIp,
                limit: MaxRequestsPerWindow,
                window: RateLimitWindow,
                cancellationToken: cancellationToken);

            if (!phoneDecision.Allowed || !ipDecision.Allowed)
            {
                var retryAfter = Math.Max(
                    1,
                    (int)Math.Ceiling(Math.Max(
                        (phoneDecision.WindowEndsAtUtc - DateTimeOffset.UtcNow).TotalSeconds,
                        (ipDecision.WindowEndsAtUtc - DateTimeOffset.UtcNow).TotalSeconds)));

                _logger.LogWarning(
                    "OTP request throttled. CorrelationId={CorrelationId} Mobile={Mobile} Ip={Ip} PhoneCount={PhoneCount} IpCount={IpCount}",
                    request.CorrelationId,
                    maskedMobile,
                    clientIp,
                    phoneDecision.CurrentCount,
                    ipDecision.CurrentCount);

                await EnforceConstantResponseTimeAsync(startedAt, cancellationToken);
                return new RequestLoginOtpResult(
                    Accepted: false,
                    Message: "تعداد درخواست ها زیاد است. لطفا کمی بعد دوباره تلاش کنید.",
                    RateLimited: true,
                    RetryAfterSeconds: retryAfter,
                    NextAllowedAtUtc: DateTimeOffset.UtcNow.AddSeconds(retryAfter));
            }

            var user = await _otpUserService.FindActiveByMobileAsync(normalizedMobile, cancellationToken);
            if (user is not null)
            {
                var otpCode = await _otpUserService.GenerateLoginOtpAsync(user.UserId, cancellationToken);
                var otpExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(2);

                var queueResult = await _otpOutboxService.QueueLoginOtpAsync(
                    new LoginOtpOutboxMessage(
                        UserId: user.UserId,
                        MobileNumber: normalizedMobile,
                        OtpCode: otpCode,
                        Purpose: "login",
                        ExpiresAtUtc: otpExpiresAtUtc,
                        CorrelationId: request.CorrelationId,
                        ClientIpAddress: clientIp,
                        UserAgent: request.UserAgent,
                        PatternCode: null),
                    cancellationToken);

                var dispatchResult = queueResult.ImmediateDispatchResult
                    ?? await _otpOutboxService.TryDispatchAsync(queueResult.OutboxMessageId, cancellationToken);

                if (!dispatchResult.Succeeded)
                {
                    _logger.LogWarning(
                        "OTP SMS delivery failed. CorrelationId={CorrelationId} Mobile={Mobile} UserId={UserId} OutboxMessageId={OutboxMessageId} ProviderCode={ProviderCode} Error={Error}",
                        request.CorrelationId,
                        maskedMobile,
                        user.UserId,
                        queueResult.OutboxMessageId,
                        dispatchResult.ProviderCode,
                        dispatchResult.ErrorMessage);

                    await EnforceConstantResponseTimeAsync(startedAt, cancellationToken);
                    return new RequestLoginOtpResult(
                        Accepted: false,
                        Message: dispatchResult.ErrorMessage ?? "ارسال کد تایید توسط سرویس پیامک انجام نشد.");
                }

                _logger.LogInformation(
                    "OTP request accepted and queued. CorrelationId={CorrelationId} Mobile={Mobile} UserId={UserId} OutboxMessageId={OutboxMessageId}",
                    request.CorrelationId,
                    maskedMobile,
                    user.UserId,
                    queueResult.OutboxMessageId);
            }
            else
            {
                _ = GenerateFakeOtpForTimingParity();
                _logger.LogInformation(
                    "OTP request accepted (non-existent account parity). CorrelationId={CorrelationId} Mobile={Mobile} Ip={Ip}",
                    request.CorrelationId,
                    maskedMobile,
                    clientIp);
            }

            await EnforceConstantResponseTimeAsync(startedAt, cancellationToken);
            return new RequestLoginOtpResult(
                Accepted: true,
                Message: "اگر حساب کاربری وجود داشته باشد، کد تایید ارسال شده است.",
                NextAllowedAtUtc: DateTimeOffset.UtcNow.AddMinutes(2));
        }

        private static async Task EnforceConstantResponseTimeAsync(Stopwatch startedAt, CancellationToken cancellationToken)
        {
            var remaining = MinimumResponseLatency - startedAt.Elapsed;
            if (remaining > TimeSpan.Zero)
            {
                await Task.Delay(remaining, cancellationToken);
            }
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

        private static string GenerateFakeOtpForTimingParity()
        {
            Span<byte> bytes = stackalloc byte[4];
            RandomNumberGenerator.Fill(bytes);
            var value = BitConverter.ToUInt32(bytes) % 100000;
            return value.ToString("D5");
        }
    }
}
