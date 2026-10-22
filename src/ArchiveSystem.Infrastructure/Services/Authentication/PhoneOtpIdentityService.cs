using ArchiveSystem.Application.Features.Authentication.Otp;
using ArchiveSystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Infrastructure.Services.Authentication
{
    public sealed class PhoneOtpIdentityService : IPhoneOtpIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public PhoneOtpIdentityService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<PhoneOtpIdentityUser?> FindActiveByMobileAsync(string mobileNumber, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.PhoneNumber == mobileNumber
                         && x.IsActive
                         && x.TwoFactorEnabled
                         && x.PhoneNumberConfirmed,
                    cancellationToken);

            return user is null
                ? null
                : new PhoneOtpIdentityUser(user.Id, user.PhoneNumber ?? mobileNumber);
        }

        public async Task<string> GeneratePhoneOtpAsync(long userId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var user = await _userManager.FindByIdAsync(userId.ToString())
                ?? throw new InvalidOperationException($"User '{userId}' was not found.");

            // Requirement: use Identity's Phone provider to generate the OTP.
            return await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider);
        }

        public async Task<bool> VerifyPhoneOtpAsync(long userId, string otpCode, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                return false;
            }

            return await _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider, otpCode);
        }
    }
}
