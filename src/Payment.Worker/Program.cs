using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Payment.Application;
using Payment.Infrastructure;
using Payment.Worker.Consumers;
using Serilog;

// Payment Worker host: runs background consumers that process payment events
// from RabbitMQ and publish outbox messages to ensure reliable event delivery.

var builder = Host.CreateDefaultBuilder(args);

// Serilog for structured logging throughout the worker
builder.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.ConfigureServices((context, services) =>
{
    // Shared application and infrastructure services (DB, messaging, domain logic)
    services.AddApplicationServices();
    services.AddInfrastructureServices(context.Configuration);

    // Background services for event consumption and outbox pattern
    services.AddHostedService<PaymentCompletedConsumer>();
    services.AddHostedService<OutboxProcessor>();
});

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Payment Worker starting...");

await host.RunAsync();
