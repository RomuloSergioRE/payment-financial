using Payment.Application.Common.Interfaces;

namespace Payment.Infrastructure.Messaging;

// No-op implementation of IMessageBus used as fallback when RabbitMQ is unavailable.
// Silently discards all messages — allows the application to run without a message broker.
public sealed class NullMessageBus : IMessageBus
{
    public Task PublishAsync<T>(T message, string routingKey,
        CancellationToken cancellationToken = default) where T : class
    {
        return Task.CompletedTask;
    }
}
