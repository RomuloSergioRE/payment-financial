namespace Payment.Application.Common.Interfaces;

// Abstraction for asynchronous message publishing (e.g. RabbitMQ, SQS).
public interface IMessageBus
{
    // Publishes a message to the given routing key.
    Task PublishAsync<T>(T message, string routingKey,
        CancellationToken cancellationToken = default)
        where T : class;
}
