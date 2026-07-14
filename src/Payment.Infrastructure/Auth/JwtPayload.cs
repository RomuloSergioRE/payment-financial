namespace Payment.Infrastructure.Auth;

// Immutable record holding the essential user claims extracted from a validated JWT token.
public sealed record JwtPayload(Guid UserId, string Role, string Plan);
