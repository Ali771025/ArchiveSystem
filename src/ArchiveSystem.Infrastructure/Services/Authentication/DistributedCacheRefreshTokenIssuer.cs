using System.Security.Cryptography;
using System.Text;
using ArchiveSystem.Application.Features.Authentication.Otp;
using Microsoft.Extensions.Caching.Distributed;

namespace ArchiveSystem.Infrastructure.Services.Authentication
{
    public sealed class DistributedCacheRefreshTokenIssuer : IRefreshTokenIssuer
    {
        private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

        private readonly IDistributedCache _cache;

        public DistributedCacheRefreshTokenIssuer(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<string> IssueAsync(long userId, CancellationToken cancellationToken = default)
        {
            var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

            await _cache.SetStringAsync(
                BuildKey(userId, tokenHash),
                "valid",
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = RefreshTokenLifetime
                },
                cancellationToken);

            return rawToken;
        }

        private static string BuildKey(long userId, string tokenHash)
        {
            return $"auth:refresh:{userId}:{tokenHash}";
        }
    }
}
