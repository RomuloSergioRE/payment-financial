using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Payment.Application.Common.Interfaces;
using Payment.Domain.Entities;
using RabbitMQ.Client;

namespace Payment.Worker.Consumers;

// Implements the Transactional Outbox pattern: polls the OutboxMessages table for
// unpublished events, publishes them to RabbitMQ, and marks them as processed/failed.
// This guarantees at-least-once delivery without tight coupling between the write
// model and the message broker.
public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly IConnection? _connection;
    private const string ExchangeName = "payment.events";
    private const int BatchSize = 10;

    public OutboxProcessor(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessor> logger,
        IConnection? connection)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _connection = connection;
    }

    // Polls for pending outbox messages every 5 seconds
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessages(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    // Fetches a batch of unprocessed messages, publishes each one, then updates their status
    private async Task ProcessPendingMessages(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IPaymentDbContext>();

        // Only pick messages that haven't been published yet, oldest first
        var messages = await context.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
            return;

        _logger.LogInformation("Processing {Count} outbox messages", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                // Publish to RabbitMQ only if the connection is available
                if (_connection is not null && _connection.IsOpen)
                {
                    PublishToRabbitMq(message);
                }

                message.MarkProcessed();
                _logger.LogInformation(
                    "Outbox message {Id} processed: {EventType}",
                    message.Id, message.EventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process outbox message {Id}", message.Id);
                message.MarkFailed(ex.Message);
            }
        }

        // Single SaveChanges persists all status updates in one DB round-trip
        await context.SaveChangesAsync(cancellationToken);
    }

    // Serializes and publishes a single outbox message to the RabbitMQ topic exchange
    private void PublishToRabbitMq(OutboxMessage message)
    {
        using var channel = _connection!.CreateModel();
        channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);

        var body = System.Text.Encoding.UTF8.GetBytes(message.Payload);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.MessageId = message.Id.ToString();
        properties.Timestamp = new AmqpTimestamp(
            DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        // Derive routing key from event type (e.g. "Payment Completed" → "payment.completed")
        var routingKey = message.EventType.ToLowerInvariant()
            .Replace(" ", ".");

        channel.BasicPublish(ExchangeName, routingKey, true, properties, body);
    }
}
