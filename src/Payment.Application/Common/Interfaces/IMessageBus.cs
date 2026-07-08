namespace Payment.Application.Common.Interfaces;

public interface IMessageBus
{
    Task PublishAsync<T>(T message, string routingKey,
        CancellationToken cancellationToken = default)
        where T : class;
}
