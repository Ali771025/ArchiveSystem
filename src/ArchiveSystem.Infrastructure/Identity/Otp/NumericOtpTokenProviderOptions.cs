using Microsoft.AspNetCore.Identity;

namespace ArchiveSystem.Infrastructure.Identity.Otp
{
    public sealed class NumericOtpTokenProviderOptions : DataProtectionTokenProviderOptions
    {
        public const string ProviderName = "NumericOtp";

        public NumericOtpTokenProviderOptions()
        {
            Name = ProviderName;
            TokenLifespan = TimeSpan.FromMinutes(2);
        }

        public int Digits { get; set; } = 5;
    }
}
