// src/ArchiveSystem.Infrastructure/Messaging/RabbitMqMessageBus.cs
using MassTransit;
using ArchiveSystem.Domain.Services;

namespace ArchiveSystem.Infrastructure.Messaging
{
    public class RabbitMqMessageBus : IMessageBus
    {
        private readonly IBus _bus;

        public RabbitMqMessageBus(IBus bus)
        {
            _bus = bus;
        }

        public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            await _bus.Publish(message, cancellationToken);
        }

        public async Task PublishAsync<T>(IEnumerable<T> messages, CancellationToken cancellationToken = default) where T : class
        {
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            foreach (var message in messages)
            {
                await _bus.Publish(message, cancellationToken);
            }
        }

        public async Task SubscribeAsync<T>(Func<T, CancellationToken, Task> handler, string queueName, CancellationToken cancellationToken = default) where T : class
        {
            // Note: This is a simplified approach. In a real-world scenario, you'd typically register consumers
            // during application startup rather than dynamically subscribing.
            // This method is more for demonstration purposes.
            
            throw new NotImplementedException("Dynamic subscription is not implemented. Register consumers during application startup.");
        }
    }
}