namespace ArchiveSystem.Infrastructure.Logging
{
    public interface ISecurityEventLogger
    {
        Task LogAsync(
            string eventType,
            string description,
            string? actorUserId,
            string? targetUserId,
            object? metadata = null,
            CancellationToken cancellationToken = default);
    }
}
