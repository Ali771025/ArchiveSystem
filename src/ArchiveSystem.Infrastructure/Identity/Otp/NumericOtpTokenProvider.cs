using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchiveSystem.Infrastructure.Identity.Otp
{
    public sealed class NumericOtpTokenProvider : DataProtectorTokenProvider<ApplicationUser>
    {
        private readonly IDataProtector _protector;
        private readonly NumericOtpTokenProviderOptions _options;

        public NumericOtpTokenProvider(
            IDataProtectionProvider dataProtectionProvider,
            IOptions<NumericOtpTokenProviderOptions> options,
            ILogger<DataProtectorTokenProvider<ApplicationUser>> logger)
            : base(
                dataProtectionProvider,
                Microsoft.Extensions.Options.Options.Create<DataProtectionTokenProviderOptions>(options.Value),
                logger)
        {
            _options = options.Value;
            _protector = dataProtectionProvider.CreateProtector(_options.Name);
        }

        public override async Task<string> GenerateAsync(string purpose, UserManager<ApplicationUser> manager, ApplicationUser user)
        {
            var otp = GenerateNumericOtp(_options.Digits);
            var securityStamp = await manager.GetSecurityStampAsync(user);

            var envelope = new OtpEnvelope(
                Code: otp,
                UserId: user.Id,
                Purpose: purpose,
                ExpiresAtUtc: DateTime.UtcNow.Add(_options.TokenLifespan),
                SecurityStamp: securityStamp ?? string.Empty);

            var serialized = JsonSerializer.Serialize(envelope);
            var protectedPayload = _protector.Protect(serialized);
            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(protectedPayload);
            var encoded = Convert.ToBase64String(payloadBytes);

            return $"{otp}.{encoded}";
        }

        public override async Task<bool> ValidateAsync(string purpose, string token, UserManager<ApplicationUser> manager, ApplicationUser user)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var parts = token.Split('.', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || !IsValidNumericCode(parts[0], _options.Digits))
            {
                return false;
            }

            try
            {
                var protectedPayload = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(parts[1]));
                var unprotectedPayload = _protector.Unprotect(protectedPayload);
                var envelope = JsonSerializer.Deserialize<OtpEnvelope>(unprotectedPayload);

                if (envelope is null)
                {
                    return false;
                }

                if (!string.Equals(envelope.Code, parts[0], StringComparison.Ordinal))
                {
                    return false;
                }

                if (envelope.UserId != user.Id)
                {
                    return false;
                }

                if (!string.Equals(envelope.Purpose, purpose, StringComparison.Ordinal))
                {
                    return false;
                }

                if (DateTime.UtcNow > envelope.ExpiresAtUtc)
                {
                    return false;
                }

                var securityStamp = await manager.GetSecurityStampAsync(user) ?? string.Empty;
                return string.Equals(securityStamp, envelope.SecurityStamp, StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        public override Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
        {
            return Task.FromResult(true);
        }

        private static string GenerateNumericOtp(int digits)
        {
            var max = (int)Math.Pow(10, digits);
            Span<byte> bytes = stackalloc byte[4];
            RandomNumberGenerator.Fill(bytes);
            var value = (int)(BitConverter.ToUInt32(bytes) % (uint)max);
            return value.ToString($"D{digits}");
        }

        private static bool IsValidNumericCode(string value, int digits)
        {
            return value.Length == digits && value.All(char.IsDigit);
        }

        private sealed record OtpEnvelope(
            string Code,
            long UserId,
            string Purpose,
            DateTime ExpiresAtUtc,
            string SecurityStamp);
    }
}
