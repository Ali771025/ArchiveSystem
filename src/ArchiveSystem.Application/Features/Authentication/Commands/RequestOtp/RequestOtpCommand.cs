using MediatR;

namespace ArchiveSystem.Application.Features.Authentication.Commands.RequestOtp
{
    public sealed record RequestOtpCommand(string MobileNumber) : IRequest<RequestOtpResult>;

    public sealed record RequestOtpResult(
        bool Succeeded,
        string Message,
        DateTimeOffset? NextAllowedAtUtc = null);
}
