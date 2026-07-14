using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Payment.Application.Common.Models;

namespace Payment.Infrastructure.Auth;

// Validates JWT tokens using HMAC-SHA256 and extracts user claims.
// Returns a Result pattern to avoid throwing exceptions on invalid tokens.
public sealed class JwtValidator
{
    private readonly JwtSecurityTokenHandler _handler;
    private readonly TokenValidationParameters _parameters;
    private readonly ILogger<JwtValidator> _logger;

    public JwtValidator(IConfiguration configuration, ILogger<JwtValidator> logger)
    {
        _handler = new JwtSecurityTokenHandler();
        _logger = logger;

        var secret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT:Secret is required");

        _parameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secret)),
            ValidateIssuer = true,
            ValidIssuer = configuration["Jwt:Issuer"] ?? "zenyfin-api",
            ValidateAudience = true,
            ValidAudience = configuration["Jwt:Audience"] ?? "zenyfin-payment",
            ValidateLifetime = true,
            // No clock skew tolerance — tokens expire exactly at their expiration time
            ClockSkew = TimeSpan.Zero
        };
    }

    // Validates a JWT token and extracts userId, role, and plan claims into a JwtPayload
    public Result<JwtPayload> ValidateToken(string token)
    {
        try
        {
            var principal = _handler.ValidateToken(token, _parameters, out var validatedToken);

            // Ensure the token uses HMAC-SHA256 algorithm
            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(
                    SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                return Result<JwtPayload>.Failure("Invalid token algorithm");
            }

            var userId = principal.FindFirst("userId")?.Value;
            var role = principal.FindFirst(ClaimTypes.Role)?.Value;
            var plan = principal.FindFirst("plan")?.Value;

            if (userId is null || role is null || plan is null)
                return Result<JwtPayload>.Failure("Missing required claims");

            return Result<JwtPayload>.Success(new JwtPayload(
                Guid.Parse(userId), role, plan));
        }
        catch (SecurityTokenExpiredException)
        {
            return Result<JwtPayload>.Failure("Token expired");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "JWT validation failed");
            return Result<JwtPayload>.Failure("Invalid token");
        }
    }
}
