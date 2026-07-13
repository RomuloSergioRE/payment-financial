using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Payment.Infrastructure.HealthChecks;

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

        if (connection is null)
            return Task.FromResult(
                HealthCheckResult.Degraded("RabbitMQ connection not configured."));

        return Task.FromResult(connection.IsOpen
            ? HealthCheckResult.Healthy("RabbitMQ is connected.")
            : HealthCheckResult.Unhealthy("RabbitMQ connection is closed."));
    }
}
