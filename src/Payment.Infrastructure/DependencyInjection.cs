using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Payment.Application.Common.Interfaces;
using Payment.Infrastructure.Auth;
using Payment.Infrastructure.Messaging;
using Payment.Infrastructure.PaymentGateway;
using Payment.Infrastructure.Persistence;
using RabbitMQ.Client;

namespace Payment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // PostgreSQL
        services.AddDbContext<PaymentDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("PaymentDatabase");
            options.UseNpgsql(connectionString);
        });
        services.AddScoped<IPaymentDbContext>(sp =>
            sp.GetRequiredService<PaymentDbContext>());

        // RabbitMQ (optional - gracefully handles unavailable broker)
        var rabbitHost = configuration["RabbitMQ:Host"];
        if (!string.IsNullOrEmpty(rabbitHost))
        {
            var useRabbit = false;
            IConnection? connection = null;

            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = rabbitHost,
                    Port = int.TryParse(configuration["RabbitMQ:Port"], out var port) ? port : 5672,
                    UserName = configuration["RabbitMQ:Username"] ?? "guest",
                    Password = configuration["RabbitMQ:Password"] ?? "guest",
                    VirtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/",
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(5),
                };
                connection = factory.CreateConnection();
                useRabbit = true;
            }
            catch (Exception ex)
            {
                var loggerFactory = services.BuildServiceProvider()
                    .GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("Payment.Infrastructure.RabbitMQ");
                logger.LogWarning(ex,
                    "Could not connect to RabbitMQ at {Host}. Message publishing will be unavailable.",
                    rabbitHost);
            }

            if (useRabbit && connection is not null)
            {
                services.AddSingleton<IConnection>(connection);
                services.AddSingleton<IMessageBus, RabbitMqBus>();
            }
            else
            {
                services.AddSingleton<IMessageBus, NullMessageBus>();
            }
        }
        else
        {
            services.AddSingleton<IMessageBus, NullMessageBus>();
        }

        // Payment Gateway
        services.AddScoped<IPaymentGateway, FakePaymentGateway>();

        // JWT
        services.AddScoped<JwtValidator>();

        return services;
    }
}

public sealed class NullMessageBus : IMessageBus
{
    public Task PublishAsync<T>(T message, string routingKey,
        CancellationToken cancellationToken = default) where T : class
    {
        return Task.CompletedTask;
    }
}
