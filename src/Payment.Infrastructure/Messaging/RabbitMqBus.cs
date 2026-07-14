using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Payment.Application.Common.Interfaces;
using Polly;
using RabbitMQ.Client;

namespace Payment.Infrastructure.Messaging;

// RabbitMQ message bus implementation using the Topic exchange pattern.
// Serializes messages to JSON and publishes with retry policy for transient failures.
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

    // Publishes a message to the specified routing key with exponential backoff retry (3 attempts)
    public async Task PublishAsync<T>(T message, string routingKey,
        CancellationToken cancellationToken = default) where T : class
    {
        // Exponential backoff: 200ms, 400ms, 800ms between retries
        var retry = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3,
                attempt => TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100),
                (ex, time, attempt, _) =>
                {
                    _logger.LogWarning(ex,
                        "RabbitMQ publish attempt {Attempt} failed for {RoutingKey}",
                        attempt, routingKey);
                });

        await retry.ExecuteAsync(async () =>
        {
            await Task.Run(() =>
            {
                // Create a short-lived channel for each publish (RabbitMQ channels are lightweight)
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
            }, cancellationToken);

            _logger.LogInformation(
                "Published message {Type} to {RoutingKey}",
                typeof(T).Name, routingKey);
        });
    }
}
