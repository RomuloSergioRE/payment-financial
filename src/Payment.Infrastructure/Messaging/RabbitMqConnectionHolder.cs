using RabbitMQ.Client;

namespace Payment.Infrastructure.Messaging;

// Singleton holder that maintains the RabbitMQ connection across the application lifetime.
// Used by DependencyInjection to share the connection and by RabbitMqHealthCheck to verify it.
public sealed class RabbitMqConnectionHolder
{
    public IConnection? Connection { get; internal set; }
}
