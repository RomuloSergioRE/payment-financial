using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Payment.Application.Common.Interfaces;
using Polly;
using RabbitMQ.Client;

namespace Payment.Infrastructure.Messaging;

public sealed class RabbitMqBus : IMessageBus
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMqBus> _logger;
    private const string ExchangeName = "payment.events";

    public RabbitMqBus(IConnection connection, ILogger<RabbitMqBus> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public Task PublishAsync<T>(T message, string routingKey,
        CancellationToken cancellationToken = default) where T : class
    {
        var retry = Policy
            .Handle<Exception>()
            .WaitAndRetry(3,
                attempt => TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100),
                (ex, time, attempt, _) =>
                {
                    _logger.LogWarning(ex,
                        "RabbitMQ publish attempt {Attempt} failed for {RoutingKey}",
                        attempt, routingKey);
                });

        retry.Execute(() =>
        {
            using var channel = _connection.CreateModel();
            channel.ExchangeDeclare(
                ExchangeName, ExchangeType.Topic, durable: true);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.MessageId = Guid.NewGuid().ToString();
            properties.Timestamp = new AmqpTimestamp(
                DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            channel.BasicPublish(
                ExchangeName, routingKey, true, properties, body);

            _logger.LogInformation(
                "Published message {Type} to {RoutingKey}",
                typeof(T).Name, routingKey);
        });

        return Task.CompletedTask;
    }
}
