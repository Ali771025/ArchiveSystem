// src/ArchiveSystem.Domain/IServices/IMessageBus.cs
namespace ArchiveSystem.Domain.Services
{
    public interface IMessageBus
    {
        Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
        Task PublishAsync<T>(IEnumerable<T> messages, CancellationToken cancellationToken = default) where T : class;
        Task SubscribeAsync<T>(Func<T, CancellationToken, Task> handler, string queueName, CancellationToken cancellationToken = default) where T : class;
    }
}