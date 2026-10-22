using MediatR;

namespace ArchiveSystem.Application.Features.Authentication.Commands.VerifyOtpAndLogin
{
    public sealed record VerifyOtpAndLoginCommand(string MobileNumber, string OtpCode) : IRequest<VerifyOtpAndLoginResult>;

    public sealed record VerifyOtpAndLoginResult(
        bool Succeeded,
        string Message,
        string? AccessToken = null,
        string? RefreshToken = null,
        DateTimeOffset? ExpiresAtUtc = null);
}
