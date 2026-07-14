using System.Security.Claims;

namespace Payment.Api.Middleware;

// Extracts the user ID from the authenticated JWT principal and stores it
// in HttpContext.Items for convenient access via the GetUserId() extension.
public sealed class JwtUserMiddleware
{
    private readonly RequestDelegate _next;

    public JwtUserMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // Try the custom "userId" claim first, then fall back to the standard NameIdentifier
            var userIdClaim = context.User.FindFirst("userId")
                ?? context.User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim?.Value is not null &&
                Guid.TryParse(userIdClaim.Value, out var userId))
            {
                context.Items["UserId"] = userId;
            }
        }

        await _next(context);
    }
}
