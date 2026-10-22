using System.Threading;
using System.Threading.Tasks;

namespace ArchiveSystem.Application.Features.Authentication.Otp
{
    public interface ILoginOtpUserService
    {
        Task<LoginOtpUser?> FindActiveByMobileAsync(string normalizedMobileNumber, CancellationToken cancellationToken = default);
        Task<string> GenerateLoginOtpAsync(long userId, CancellationToken cancellationToken = default);
        Task<bool> VerifyLoginOtpAsync(long userId, string otpCode, CancellationToken cancellationToken = default);
        Task<string> GeneratePasswordResetOtpAsync(long userId, CancellationToken cancellationToken = default);
        Task<bool> VerifyPasswordResetOtpAsync(long userId, string otpCode, CancellationToken cancellationToken = default);
    }

    public sealed record LoginOtpUser(long UserId, string MobileNumber, bool IsActive);
}
