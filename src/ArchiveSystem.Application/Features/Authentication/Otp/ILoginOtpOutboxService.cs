using System;
using System.Threading;
using System.Threading.Tasks;

namespace ArchiveSystem.Application.Features.Authentication.Otp
{
    public interface ILoginOtpOutboxService
    {
        Task<LoginOtpQueueResult> QueueLoginOtpAsync(LoginOtpOutboxMessage message, CancellationToken cancellationToken = default);
        Task<LoginOtpDispatchResult> TryDispatchAsync(Guid outboxMessageId, CancellationToken cancellationToken = default);
    }

    public sealed record LoginOtpQueueResult(
        Guid OutboxMessageId,
        LoginOtpDispatchResult? ImmediateDispatchResult = null);

    public sealed record LoginOtpDispatchResult(
        bool Succeeded,
        string? ProviderCode = null,
        string? ErrorMessage = null);

    public sealed record LoginOtpOutboxMessage(
        long UserId,
        string MobileNumber,
        string OtpCode,
        string Purpose,
        DateTimeOffset ExpiresAtUtc,
        string CorrelationId,
        string? ClientIpAddress,
        string? UserAgent,
        string? PatternCode);
}
