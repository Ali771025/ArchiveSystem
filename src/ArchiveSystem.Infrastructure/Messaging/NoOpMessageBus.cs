using ArchiveSystem.Domain.Services;
using Microsoft.Extensions.Logging;

namespace ArchiveSystem.Infrastructure.Messaging
{
    public sealed class NoOpMessageBus : IMessageBus
    {
        private readonly ILogger<NoOpMessageBus> _logger;

        public NoOpMessageBus(ILogger<NoOpMessageBus> logger)
        {
            _logger = logger;
        }

        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
        {
            _logger.LogDebug("Messaging is disabled. Skipping publish for message type {MessageType}.", typeof(T).Name);
            return Task.CompletedTask;
        }

        public Task PublishAsync<T>(IEnumerable<T> messages, CancellationToken cancellationToken = default) where T : class
        {
            _logger.LogDebug("Messaging is disabled. Skipping publish for a message batch of type {MessageType}.", typeof(T).Name);
            return Task.CompletedTask;
        }

        public Task SubscribeAsync<T>(Func<T, CancellationToken, Task> handler, string queueName, CancellationToken cancellationToken = default) where T : class
        {
            _logger.LogDebug("Messaging is disabled. Subscription request for {QueueName} is ignored.", queueName);
            return Task.CompletedTask;
        }
    }
}
