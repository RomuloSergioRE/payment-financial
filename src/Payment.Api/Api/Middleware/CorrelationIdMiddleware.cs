using Serilog.Context;

namespace Payment.Api.Middleware;

// Propagates a correlation ID across the request pipeline.
// Reads X-Request-ID from the incoming header (or generates one),
// stores it in HttpContext.Items, echoes it back in the response,
// and pushes it into the Serilog log context for all downstream log events.
public sealed class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Request-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
        => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // Reuse existing correlation ID from the client, or create a new one
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Push the correlation ID into Serilog's ambient log context so all
        // log entries within this request include it automatically
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
