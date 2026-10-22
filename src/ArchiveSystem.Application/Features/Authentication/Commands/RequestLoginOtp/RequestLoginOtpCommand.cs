using MediatR;

namespace ArchiveSystem.Application.Features.Authentication.Commands.RequestLoginOtp
{
    public sealed record RequestLoginOtpCommand(
        string MobileNumber,
        string? ClientIpAddress,
        string? UserAgent,
        string CorrelationId) : IRequest<RequestLoginOtpResult>;

    public sealed record RequestLoginOtpResult(
        bool Accepted,
        string Message,
        bool RateLimited = false,
        int? RetryAfterSeconds = null,
        DateTimeOffset? NextAllowedAtUtc = null);
}
