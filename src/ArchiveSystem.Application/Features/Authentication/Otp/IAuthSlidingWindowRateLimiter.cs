using System;
using System.Threading;
using System.Threading.Tasks;

namespace ArchiveSystem.Application.Features.Authentication.Otp
{
    public interface IAuthSlidingWindowRateLimiter
    {
        Task<RateLimitDecision> EvaluateAndConsumeAsync(
            string dimension,
            string identifier,
            int limit,
            TimeSpan window,
            CancellationToken cancellationToken = default);
    }

    public sealed record RateLimitDecision(
        bool Allowed,
        int CurrentCount,
        DateTimeOffset WindowEndsAtUtc);
}
