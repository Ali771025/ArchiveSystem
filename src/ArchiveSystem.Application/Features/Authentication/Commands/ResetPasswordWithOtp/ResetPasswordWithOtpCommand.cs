using MediatR;

namespace ArchiveSystem.Application.Features.Authentication.Commands.ResetPasswordWithOtp
{
    public sealed record ResetPasswordWithOtpCommand(
        string MobileNumber,
        string OtpCode,
        string NewPassword) : IRequest<ResetPasswordWithOtpResult>;

    public sealed record ResetPasswordWithOtpResult(bool Succeeded, string Message);
}
