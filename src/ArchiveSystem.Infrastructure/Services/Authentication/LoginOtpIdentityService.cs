using System.Collections.Concurrent;
using ArchiveSystem.Application.Features.Authentication.Otp;
using ArchiveSystem.Infrastructure.Identity;
using ArchiveSystem.Infrastructure.Identity.Otp;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ArchiveSystem.Infrastructure.Services.Authentication
{
    public sealed class LoginOtpIdentityService : ILoginOtpUserService
    {
        private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(2);
        private static readonly ConcurrentDictionary<string, CachedOtpToken> LocalOtpFallback = new();

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDistributedCache _cache;
        private readonly ILogger<LoginOtpIdentityService> _logger;

        public LoginOtpIdentityService(
            UserManager<ApplicationUser> userManager,
            IDistributedCache cache,
            ILogger<LoginOtpIdentityService> logger)
        {
            _userManager = userManager;
            _cache = cache;
            _logger = logger;
        }

        public async Task<LoginOtpUser?> FindActiveByMobileAsync(string normalizedMobileNumber, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.PhoneNumber == normalizedMobileNumber && x.IsActive,
                    cancellationToken);

            return user is null
                ? null
                : new LoginOtpUser(user.Id, user.PhoneNumber ?? normalizedMobileNumber, user.IsActive);
        }

        public async Task<string> GenerateLoginOtpAsync(long userId, CancellationToken cancellationToken = default)
        {
            return await GenerateOtpAsync(userId, AuthOtpPurposes.Login, cancellationToken);
        }

        public async Task<string> GeneratePasswordResetOtpAsync(long userId, CancellationToken cancellationToken = default)
        {
            return await GenerateOtpAsync(userId, AuthOtpPurposes.PasswordReset, cancellationToken);
        }

        public async Task<bool> VerifyLoginOtpAsync(long userId, string otpCode, CancellationToken cancellationToken = default)
        {
            return await VerifyOtpAsync(userId, otpCode, AuthOtpPurposes.Login, cancellationToken);
        }

        public async Task<bool> VerifyPasswordResetOtpAsync(long userId, string otpCode, CancellationToken cancellationToken = default)
        {
            return await VerifyOtpAsync(userId, otpCode, AuthOtpPurposes.PasswordReset, cancellationToken);
        }

        private async Task<string> GenerateOtpAsync(long userId, string purpose, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                throw new InvalidOperationException($"User '{userId}' not found for OTP generation.");
            }

            var token = await _userManager.GenerateUserTokenAsync(user, NumericOtpTokenProviderOptions.ProviderName, purpose);
            var parts = token.Split('.', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                throw new InvalidOperationException("Generated OTP token has an invalid format.");
            }

            var cacheKey = BuildCacheKey(user.Id, purpose);
            try
            {
                await _cache.SetStringAsync(
                    cacheKey,
                    token,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = OtpTtl
                    },
                    cancellationToken);

                _logger.LogDebug("Stored full OTP token in cache for user {UserId} with a 2-minute TTL.", user.Id);
            }
            catch (Exception ex) when (IsCacheUnavailable(ex))
            {
                LocalOtpFallback[cacheKey] = new CachedOtpToken(token, DateTimeOffset.UtcNow.Add(OtpTtl));
                _logger.LogWarning(
                    ex,
                    "OTP cache is unavailable. Stored OTP token in local process fallback. UserId={UserId} Purpose={Purpose}",
                    user.Id,
                    purpose);
            }

            return parts[0];
        }

        private async Task<bool> VerifyOtpAsync(long userId, string otpCode, string purpose, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                return false;
            }

            var cacheKey = BuildCacheKey(user.Id, purpose);
            var token = await GetCachedOtpTokenAsync(cacheKey, cancellationToken);
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var parts = token.Split('.', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || !string.Equals(parts[0], otpCode, StringComparison.Ordinal))
            {
                return false;
            }

            var isValid = await _userManager.VerifyUserTokenAsync(
                user,
                NumericOtpTokenProviderOptions.ProviderName,
                purpose,
                token);

            if (isValid)
            {
                await RemoveCachedOtpTokenAsync(cacheKey, cancellationToken);
            }

            return isValid;
        }

        public static string BuildCacheKey(long userId, string purpose)
        {
            return $"auth:otp:{purpose}:{userId}";
        }

        private async Task<string?> GetCachedOtpTokenAsync(string cacheKey, CancellationToken cancellationToken)
        {
            try
            {
                var cachedToken = await _cache.GetStringAsync(cacheKey, cancellationToken);
                return cachedToken ?? GetLocalFallbackToken(cacheKey);
            }
            catch (Exception ex) when (IsCacheUnavailable(ex))
            {
                _logger.LogWarning(
                    ex,
                    "OTP cache is unavailable. Reading OTP token from local process fallback. Key={CacheKey}",
                    cacheKey);

                return GetLocalFallbackToken(cacheKey);
            }
        }

        private static string? GetLocalFallbackToken(string cacheKey)
        {
            if (!LocalOtpFallback.TryGetValue(cacheKey, out var fallbackToken))
            {
                return null;
            }

            if (fallbackToken.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            {
                LocalOtpFallback.TryRemove(cacheKey, out _);
                return null;
            }

            return fallbackToken.Token;
        }

        private async Task RemoveCachedOtpTokenAsync(string cacheKey, CancellationToken cancellationToken)
        {
            LocalOtpFallback.TryRemove(cacheKey, out _);

            try
            {
                await _cache.RemoveAsync(cacheKey, cancellationToken);
            }
            catch (Exception ex) when (IsCacheUnavailable(ex))
            {
                _logger.LogWarning(
                    ex,
                    "OTP cache is unavailable while removing consumed OTP token. Key={CacheKey}",
                    cacheKey);
            }
        }

        private static bool IsCacheUnavailable(Exception exception)
        {
            for (var current = exception; current is not null; current = current.InnerException)
            {
                if (current is RedisConnectionException or RedisTimeoutException)
                {
                    return true;
                }
            }

            return false;
        }

        private sealed record CachedOtpToken(string Token, DateTimeOffset ExpiresAtUtc);
    }
}
