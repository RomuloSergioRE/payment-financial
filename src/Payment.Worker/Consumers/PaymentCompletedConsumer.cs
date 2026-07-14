using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Payment.Worker.Consumers;

// Background consumer that listens for payment.completed events from RabbitMQ.
// Declares the exchange/queue, consumes messages with manual ACK, and logs processing details.
public sealed class PaymentCompletedConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private readonly ILogger<PaymentCompletedConsumer> _logger;
    private const string ExchangeName = "payment.events";
    private const string QueueName = "payment.completed.queue";
    private const string RoutingKey = "payment.completed";

    public PaymentCompletedConsumer(
        IConnection connection,
        ILogger<PaymentCompletedConsumer> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Create a channel and declare the topic exchange and durable queue
        var channel = _connection.CreateModel();

        channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);
        channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(QueueName, ExchangeName, RoutingKey);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (model, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());

            try
            {
                _logger.LogInformation(
                    "Received payment.completed event: {Body}", body);

                // Parse the event payload and extract key fields for logging
                var payload = JsonSerializer.Deserialize<JsonElement>(body);

                if (payload.TryGetProperty("data", out var data))
                {
                    var paymentId = data.TryGetProperty("paymentId", out var pid)
                        ? pid.GetString() : "unknown";
                    var userId = data.TryGetProperty("userId", out var uid)
                        ? uid.GetString() : "unknown";
                    var planType = data.TryGetProperty("planType", out var pt)
                        ? pt.GetString() : "unknown";

                    _logger.LogInformation(
                        "Processing completed payment: PaymentId={PaymentId}, UserId={UserId}, Plan={Plan}",
                        paymentId, userId, planType);
                }

                await Task.CompletedTask;
                // Manual ACK: message processed successfully, remove from queue
                channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment.completed event");
                // Nack with requeue so the message can be retried later
                channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        channel.BasicConsume(QueueName, autoAck: false, consumer);

        _logger.LogInformation(
            "PaymentCompletedConsumer started. Listening on queue: {Queue}", QueueName);

        return Task.CompletedTask;
    }
}
