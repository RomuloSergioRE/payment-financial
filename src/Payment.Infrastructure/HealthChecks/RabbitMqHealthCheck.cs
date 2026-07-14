using Microsoft.Extensions.Diagnostics.HealthChecks;
using Payment.Infrastructure.Messaging;

namespace Payment.Infrastructure.HealthChecks;

// Health check that verifies RabbitMQ connection status.
// Returns Degraded if no connection is configured (optional dependency),
// Unhealthy if the connection exists but is closed.
public sealed class RabbitMqHealthCheck : IHealthCheck
{
    private readonly RabbitMqConnectionHolder _connectionHolder;

    public RabbitMqHealthCheck(RabbitMqConnectionHolder connectionHolder)
        => _connectionHolder = connectionHolder;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var connection = _connectionHolder.Connection;

        // Connection is null when RabbitMQ was not configured or failed to connect at startup
        if (connection is null)
            return Task.FromResult(
                HealthCheckResult.Degraded("RabbitMQ connection not configured."));

        return Task.FromResult(connection.IsOpen
            ? HealthCheckResult.Healthy("RabbitMQ is connected.")
            : HealthCheckResult.Unhealthy("RabbitMQ connection is closed."));
    }
}
