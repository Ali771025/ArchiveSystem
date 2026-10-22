using System.Text.Json;
using ArchiveSystem.Application.Features.Authentication.Otp;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Infrastructure.Integrations.MeliPayamak;
using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchiveSystem.Infrastructure.Services.Authentication
{
    public sealed class LoginOtpOutboxService : ILoginOtpOutboxService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly ArchiveDbContext _dbContext;
        private readonly IMeliPayamakService _meliPayamakService;
        private readonly IOptions<MeliPayamakOptions> _options;
        private readonly ILogger<LoginOtpOutboxService> _logger;

        public LoginOtpOutboxService(
            ArchiveDbContext dbContext,
            IMeliPayamakService meliPayamakService,
            IOptions<MeliPayamakOptions> options,
            ILogger<LoginOtpOutboxService> logger)
        {
            _dbContext = dbContext;
            _meliPayamakService = meliPayamakService;
            _options = options;
            _logger = logger;
        }

        public async Task<LoginOtpQueueResult> QueueLoginOtpAsync(LoginOtpOutboxMessage message, CancellationToken cancellationToken = default)
        {
            var payload = new LoginOtpPayload(message.OtpCode, message.ExpiresAtUtc);
            var entity = new SmsOutboxMessage
            {
                Id = Guid.NewGuid(),
                UserId = message.UserId,
                MobileNumber = message.MobileNumber,
                Purpose = message.Purpose,
                PatternCode = string.IsNullOrWhiteSpace(message.PatternCode)
                    ? _options.Value.LoginOtpPatternCode
                    : message.PatternCode!,
                PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
                CorrelationId = message.CorrelationId,
                ClientIpAddress = message.ClientIpAddress,
                UserAgent = Truncate(message.UserAgent, 512),
                CreatedAtUtc = DateTime.UtcNow,
                NextAttemptAtUtc = DateTime.UtcNow,
                Status = SmsOutboxStatus.Pending,
                MaxAttempts = 5
            };

            _dbContext.Set<SmsOutboxMessage>().Add(entity);
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsMissingSmsOutboxTable(ex))
            {
                _logger.LogWarning(
                    ex,
                    "SMS outbox table is missing. Sending OTP directly without outbox persistence. UserId={UserId} CorrelationId={CorrelationId} Mobile={Mobile}",
                    message.UserId,
                    message.CorrelationId,
                    MaskMobile(message.MobileNumber));

                _dbContext.Entry(entity).State = EntityState.Detached;
                var dispatchResult = await SendDirectAsync(message.MobileNumber, entity.PatternCode, payload.Code, cancellationToken);
                return new LoginOtpQueueResult(Guid.Empty, dispatchResult);
            }

            _logger.LogInformation(
                "Queued OTP SMS in outbox. OutboxMessageId={OutboxMessageId} UserId={UserId} CorrelationId={CorrelationId} Mobile={Mobile}",
                entity.Id,
                entity.UserId,
                entity.CorrelationId,
                MaskMobile(entity.MobileNumber));

            return new LoginOtpQueueResult(entity.Id);
        }

        public async Task<LoginOtpDispatchResult> TryDispatchAsync(Guid outboxMessageId, CancellationToken cancellationToken = default)
        {
            if (outboxMessageId == Guid.Empty)
            {
                return new LoginOtpDispatchResult(false, ErrorMessage: "شناسه پیامک Outbox معتبر نیست.");
            }

            var entity = await _dbContext.Set<SmsOutboxMessage>()
                .FirstOrDefaultAsync(x => x.Id == outboxMessageId, cancellationToken);

            if (entity is null)
            {
                _logger.LogWarning("Outbox message {OutboxMessageId} was not found for dispatch.", outboxMessageId);
                return new LoginOtpDispatchResult(false, ErrorMessage: "پیام Outbox برای ارسال یافت نشد.");
            }

            if (entity.Status is SmsOutboxStatus.Sent or SmsOutboxStatus.DeadLetter)
            {
                return entity.Status == SmsOutboxStatus.Sent
                    ? new LoginOtpDispatchResult(true)
                    : new LoginOtpDispatchResult(false, ErrorMessage: entity.LastError);
            }

            if (entity.NextAttemptAtUtc.HasValue && entity.NextAttemptAtUtc.Value > DateTime.UtcNow)
            {
                return new LoginOtpDispatchResult(false, ErrorMessage: entity.LastError ?? "زمان تلاش مجدد برای ارسال پیامک هنوز نرسیده است.");
            }

            LoginOtpPayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<LoginOtpPayload>(entity.PayloadJson, JsonOptions);
            }
            catch (Exception ex)
            {
                entity.Status = SmsOutboxStatus.DeadLetter;
                entity.LastError = Truncate($"Invalid outbox payload: {ex.Message}", 1000);
                entity.AttemptCount++;
                await _dbContext.SaveChangesAsync(cancellationToken);
                return new LoginOtpDispatchResult(false, ErrorMessage: entity.LastError);
            }

            if (payload is null)
            {
                entity.Status = SmsOutboxStatus.DeadLetter;
                entity.LastError = "Outbox payload is empty.";
                entity.AttemptCount++;
                await _dbContext.SaveChangesAsync(cancellationToken);
                return new LoginOtpDispatchResult(false, ErrorMessage: entity.LastError);
            }

            MeliPayamakSendResult sendResult;
            try
            {
                sendResult = await _meliPayamakService.SendPatternAsync(
                    new MeliPayamakPatternRequest(
                        MobileNumber: entity.MobileNumber,
                        PatternCode: entity.PatternCode,
                        Tokens: new[] { payload.Code }),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "MeliPayamak dispatch failed for outbox message {OutboxMessageId}. Mobile={Mobile}",
                    entity.Id,
                    MaskMobile(entity.MobileNumber));

                sendResult = new MeliPayamakSendResult(false, null, ex.Message);
            }

            entity.AttemptCount++;
            if (sendResult.Succeeded)
            {
                entity.Status = SmsOutboxStatus.Sent;
                entity.SentAtUtc = DateTime.UtcNow;
                entity.LastError = null;
                entity.NextAttemptAtUtc = null;
            }
            else
            {
                entity.Status = entity.AttemptCount >= entity.MaxAttempts
                    ? SmsOutboxStatus.DeadLetter
                    : SmsOutboxStatus.Failed;

                entity.LastError = Truncate(sendResult.ErrorMessage, 1000);

                if (entity.Status == SmsOutboxStatus.Failed)
                {
                    var delaySeconds = Math.Min(300, (int)Math.Pow(2, entity.AttemptCount));
                    entity.NextAttemptAtUtc = DateTime.UtcNow.AddSeconds(delaySeconds);
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Processed OTP outbox message. OutboxMessageId={OutboxMessageId} Status={Status} Attempt={AttemptCount}/{MaxAttempts}",
                entity.Id,
                entity.Status,
                entity.AttemptCount,
                entity.MaxAttempts);

            return new LoginOtpDispatchResult(
                sendResult.Succeeded,
                sendResult.ProviderTrackingId,
                sendResult.ErrorMessage);
        }

        private static string MaskMobile(string mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile) || mobile.Length < 7)
            {
                return "***";
            }

            return $"{mobile[..4]}****{mobile[^3..]}";
        }

        private async Task<LoginOtpDispatchResult> SendDirectAsync(
            string mobileNumber,
            string patternCode,
            string otpCode,
            CancellationToken cancellationToken)
        {
            var sendResult = await _meliPayamakService.SendPatternAsync(
                new MeliPayamakPatternRequest(
                    MobileNumber: mobileNumber,
                    PatternCode: patternCode,
                    Tokens: new[] { otpCode }),
                cancellationToken);

            if (!sendResult.Succeeded)
            {
                _logger.LogWarning(
                    "Direct OTP SMS dispatch failed. Mobile={Mobile} ProviderTrackingId={ProviderTrackingId} Error={Error}",
                    MaskMobile(mobileNumber),
                    sendResult.ProviderTrackingId,
                    sendResult.ErrorMessage);
            }

            return new LoginOtpDispatchResult(
                sendResult.Succeeded,
                sendResult.ProviderTrackingId,
                sendResult.ErrorMessage);
        }

        private static bool IsMissingSmsOutboxTable(Exception exception)
        {
            for (var current = exception; current is not null; current = current.InnerException)
            {
                if (current is SqlException sqlException &&
                    sqlException.Number == 208 &&
                    sqlException.Message.Contains("SmsOutboxMessages", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string? Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value[..maxLength];
        }

        private sealed record LoginOtpPayload(string Code, DateTimeOffset ExpiresAtUtc);
    }
}
