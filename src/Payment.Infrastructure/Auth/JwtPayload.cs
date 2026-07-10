namespace Payment.Infrastructure.Auth;

public sealed record JwtPayload(Guid UserId, string Role, string Plan);
