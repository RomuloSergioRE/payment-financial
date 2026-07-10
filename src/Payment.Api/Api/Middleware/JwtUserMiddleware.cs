using System.Security.Claims;

namespace Payment.Api.Middleware;

public sealed class JwtUserMiddleware
{
    private readonly RequestDelegate _next;

    public JwtUserMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
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
