using System.Text.Json;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace ArchiveSystem.Infrastructure.Logging
{
    public sealed class SecurityEventLogger : ISecurityEventLogger
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ArchiveDbContext _dbContext;
        private readonly ILogger<SecurityEventLogger> _logger;

        public SecurityEventLogger(
            IHttpContextAccessor httpContextAccessor,
            ArchiveDbContext dbContext,
            ILogger<SecurityEventLogger> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task LogAsync(
            string eventType,
            string description,
            string? actorUserId,
            string? targetUserId,
            object? metadata = null,
            CancellationToken cancellationToken = default)
        {
            var ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
            var metadataJson = metadata == null ? null : JsonSerializer.Serialize(metadata);

            _dbContext.SecurityLogs.Add(new SecurityLog
            {
                EventType = eventType.Trim(),
                Message = description,
                Level = "Information",
                ActionDate = DateTime.UtcNow,
                ActorUserId = actorUserId,
                TargetUserId = targetUserId,
                ClientIpAddress = ip,
                SecurityMetadata = metadataJson
            });

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Security event persisted: {SecurityEventType}. {SecurityDescription}",
                eventType,
                description);
        }
    }
}
