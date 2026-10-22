namespace ArchiveSystem.Application.Features.Authentication.Otp
{
    public interface IPhoneOtpIdentityService
    {
        Task<PhoneOtpIdentityUser?> FindActiveByMobileAsync(string mobileNumber, CancellationToken cancellationToken = default);
        Task<string> GeneratePhoneOtpAsync(long userId, CancellationToken cancellationToken = default);
        Task<bool> VerifyPhoneOtpAsync(long userId, string otpCode, CancellationToken cancellationToken = default);
    }

    public sealed record PhoneOtpIdentityUser(long UserId, string MobileNumber);
}
