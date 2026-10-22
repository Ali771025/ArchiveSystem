using MediatR;

namespace ArchiveSystem.Application.Features.Authentication.Commands.RequestPasswordResetOtp
{
    public sealed record RequestPasswordResetOtpCommand(
        string MobileNumber,
        string? ClientIpAddress,
        string? UserAgent,
        string CorrelationId) : IRequest<RequestPasswordResetOtpResult>;

    public sealed record RequestPasswordResetOtpResult(
        bool Accepted,
        string Message,
        bool RateLimited = false,
        int? RetryAfterSeconds = null);
}
