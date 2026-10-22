using System.Text.Json;
using ArchiveSystem.Application.Features.Authentication.Otp;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ArchiveSystem.Infrastructure.Security
{
    public sealed class RedisSlidingWindowRateLimiter : IAuthSlidingWindowRateLimiter
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IDistributedCache _cache;
        private readonly ILogger<RedisSlidingWindowRateLimiter> _logger;

        public RedisSlidingWindowRateLimiter(
            IDistributedCache cache,
            ILogger<RedisSlidingWindowRateLimiter> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<RateLimitDecision> EvaluateAndConsumeAsync(
            string dimension,
            string identifier,
            int limit,
            TimeSpan window,
            CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.UtcNow;
            var key = BuildKey(dimension, identifier);
            string? raw;
            try
            {
                raw = await _cache.GetStringAsync(key, cancellationToken);
            }
            catch (Exception ex) when (IsCacheUnavailable(ex))
            {
                _logger.LogWarning(
                    ex,
                    "OTP rate limiter cache is unavailable. Allowing request without rate-limit state. Dimension={Dimension} Key={Key}",
                    dimension,
                    key);

                return new RateLimitDecision(true, 0, now.Add(window));
            }

            var timestamps = string.IsNullOrWhiteSpace(raw)
                ? new List<long>()
                : JsonSerializer.Deserialize<List<long>>(raw, JsonOptions) ?? new List<long>();

            var windowStartUnixMs = now.Add(-window).ToUnixTimeMilliseconds();
            timestamps.RemoveAll(value => value < windowStartUnixMs);

            var allowed = timestamps.Count < limit;
            if (allowed)
            {
                timestamps.Add(now.ToUnixTimeMilliseconds());
            }

            var updated = JsonSerializer.Serialize(timestamps, JsonOptions);
            try
            {
                await _cache.SetStringAsync(
                    key,
                    updated,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = window + TimeSpan.FromMinutes(1)
                    },
                    cancellationToken);
            }
            catch (Exception ex) when (IsCacheUnavailable(ex))
            {
                _logger.LogWarning(
                    ex,
                    "OTP rate limiter cache write failed. Continuing request without persisted rate-limit state. Dimension={Dimension} Key={Key}",
                    dimension,
                    key);
            }

            var windowEndsAtUtc = timestamps.Count == 0
                ? now.Add(window)
                : DateTimeOffset.FromUnixTimeMilliseconds(timestamps.Min()).Add(window);

            _logger.LogDebug(
                "Evaluated sliding-window limit. Dimension={Dimension} Key={Key} Allowed={Allowed} Count={Count} Limit={Limit}",
                dimension,
                key,
                allowed,
                timestamps.Count,
                limit);

            return new RateLimitDecision(allowed, timestamps.Count, windowEndsAtUtc);
        }

        private static string BuildKey(string dimension, string identifier)
        {
            return $"ratelimit:{dimension}:{identifier.Trim().ToLowerInvariant()}";
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
    }
}
