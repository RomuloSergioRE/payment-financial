namespace Payment.Api.Middleware;

// Validates the Origin/Referer headers on state-changing requests (POST, PUT, DELETE)
// against a configured allowlist to prevent CSRF from untrusted origins.
public sealed class OriginValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public OriginValidationMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only enforce origin checks on state-changing HTTP methods
        if (HttpMethods.IsPost(context.Request.Method) ||
            HttpMethods.IsPut(context.Request.Method) ||
            HttpMethods.IsDelete(context.Request.Method))
        {
            var origin = context.Request.Headers["Origin"].FirstOrDefault();
            var referer = context.Request.Headers["Referer"].FirstOrDefault();

            var allowedOrigins = _configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? ["http://localhost:3000"];

            // Validate the Origin header first (preferred for CORS preflight requests)
            if (!string.IsNullOrEmpty(origin))
            {
                if (!allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(
                        System.Text.Json.JsonSerializer.Serialize(new { error = "Origin not allowed" }));
                    return;
                }
            }
            // Fall back to extracting the origin from the Referer header
            else if (!string.IsNullOrEmpty(referer))
            {
                var refererOrigin = new Uri(referer).GetLeftPart(UriPartial.Authority);
                if (!allowedOrigins.Contains(refererOrigin, StringComparer.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(
                        System.Text.Json.JsonSerializer.Serialize(new { error = "Referer not allowed" }));
                    return;
                }
            }
        }

        await _next(context);
    }
}
