using Microsoft.AspNetCore.Http;

namespace Payment.Api.Api.Extensions;

// Provides a typed helper to extract the user ID stored by JwtUserMiddleware.
public static class HttpContextExtensions
{
    private const string UserIdKey = "UserId";

    // Retrieves the authenticated user's ID from HttpContext.Items.
    // Returns null if the middleware did not set it (e.g. unauthenticated request).
    public static Guid? GetUserId(this HttpContext context)
    {
        if (context.Items.TryGetValue(UserIdKey, out var value) && value is Guid userId)
            return userId;

        return null;
    }
}
