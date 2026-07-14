using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Payment.Api.Controllers;

// Health check endpoints for liveness and readiness probes.
[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;

    public HealthController(HealthCheckService healthCheckService)
        => _healthCheckService = healthCheckService;

    // Runs all registered health checks and returns detailed status per dependency.
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get()
    {
        var result = await _healthCheckService.CheckHealthAsync();

        return Ok(new
        {
            status = result.Status.ToString().ToLowerInvariant(),
            service = "payment-financial",
            timestamp = DateTime.UtcNow,
            checks = result.Entries.ToDictionary(
                e => e.Key,
                e => new
                {
                    status = e.Value.Status.ToString().ToLowerInvariant(),
                    description = e.Value.Description,
                    duration = e.Value.Duration.ToString()
                })
        });
    }

    // Lightweight liveness probe that always returns 200 if the process is running.
    [HttpGet("live")]
    [AllowAnonymous]
    public IActionResult Live()
    {
        return Ok(new
        {
            status = "healthy",
            service = "payment-financial"
        });
    }
}
