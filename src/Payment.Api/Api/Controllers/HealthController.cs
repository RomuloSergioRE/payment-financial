using Microsoft.AspNetCore.Mvc;

namespace Payment.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            service = "payment-financial",
            timestamp = DateTime.UtcNow
        });
    }
}
