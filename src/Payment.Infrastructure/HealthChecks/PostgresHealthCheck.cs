using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.HealthChecks;

// Health check that verifies PostgreSQL connectivity by executing a simple SELECT 1 query.
public sealed class PostgresHealthCheck : IHealthCheck
{
    private readonly PaymentDbContext _context;

    public PostgresHealthCheck(PaymentDbContext context)
        => _context = context;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Minimal query to verify the database connection is alive
            await _context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            return HealthCheckResult.Healthy("PostgreSQL is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL is unreachable.", ex);
        }
    }
}
