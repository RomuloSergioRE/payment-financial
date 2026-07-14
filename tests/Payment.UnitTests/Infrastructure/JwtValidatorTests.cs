using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Payment.Infrastructure.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Payment.UnitTests.Infrastructure;

// Tests for the JwtValidator: token validation, expiry, signature, issuer, and claims verification.
public class JwtValidatorTests : IDisposable
{
    private const string TestSecret = "test-secret-key-for-jwt-validation-tests-32ch";
    private readonly JwtValidator _validator;

    public JwtValidatorTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = TestSecret,
                ["Jwt:Issuer"] = "zenyfin-api",
                ["Jwt:Audience"] = "zenyfin-payment"
            })
            .Build();

        _validator = new JwtValidator(
            configuration,
            Mock.Of<ILogger<JwtValidator>>());
    }

    private static string GenerateValidToken(
        Guid? userId = null,
        string? role = null,
        string? plan = null,
        DateTime? expiry = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new("userId", (userId ?? Guid.NewGuid()).ToString()),
            new(ClaimTypes.Role, role ?? "user"),
            new("plan", plan ?? "pro")
        };

        var token = new JwtSecurityToken(
            issuer: "zenyfin-api",
            audience: "zenyfin-payment",
            claims: claims,
            expires: expiry ?? DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Given a valid token with all required claims, When validated, Then a successful result with role and plan is returned.
    [Fact]
    public void ValidToken_ReturnsSuccess()
    {
        // Arrange
        var token = GenerateValidToken(
            userId: Guid.NewGuid(), role: "admin", plan: "enterprise");

        // Act
        var result = _validator.ValidateToken(token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Role.Should().Be("admin");
        result.Value.Plan.Should().Be("enterprise");
    }

    // Given an expired token, When validated, Then a failure result with "Token expired" is returned.
    [Fact]
    public void ExpiredToken_ReturnsFailure()
    {
        // Arrange
        var token = GenerateValidToken(
            expiry: DateTime.UtcNow.AddMinutes(-10));

        // Act
        var result = _validator.ValidateToken(token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Token expired");
    }

    // Given a token signed with a wrong key, When validated, Then a failure result with "Invalid token" is returned.
    [Fact]
    public void InvalidSignature_ReturnsFailure()
    {
        // Arrange
        var wrongKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("wrong-secret-key-32-chars-long!!!!!!!!"));
        var creds = new SigningCredentials(wrongKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "zenyfin-api",
            audience: "zenyfin-payment",
            claims: new[] { new Claim("userId", Guid.NewGuid().ToString()) },
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        // Act
        var result = _validator.ValidateToken(tokenString);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid token");
    }

    // Given a token missing the userId claim, When validated, Then a failure result with "Missing required claims" is returned.
    [Fact]
    public void MissingUserId_ReturnsFailure()
    {
        // Arrange
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "zenyfin-api",
            audience: "zenyfin-payment",
            claims: new[]
            {
                new Claim(ClaimTypes.Role, "user"),
                new Claim("plan", "pro")
            },
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        // Act
        var result = _validator.ValidateToken(tokenString);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Missing required claims");
    }

    // Given a token missing the role claim, When validated, Then a failure result with "Missing required claims" is returned.
    [Fact]
    public void MissingRole_ReturnsFailure()
    {
        // Arrange
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "zenyfin-api",
            audience: "zenyfin-payment",
            claims: new[]
            {
                new Claim("userId", Guid.NewGuid().ToString()),
                new Claim("plan", "pro")
            },
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        // Act
        var result = _validator.ValidateToken(tokenString);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Missing required claims");
    }

    // Given a token with a wrong issuer, When validated, Then a failure result with "Invalid token" is returned.
    [Fact]
    public void WrongIssuer_ReturnsFailure()
    {
        // Arrange
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "wrong-issuer",
            audience: "zenyfin-payment",
            claims: new[] { new Claim("userId", Guid.NewGuid().ToString()) },
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        // Act
        var result = _validator.ValidateToken(tokenString);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid token");
    }

    // Given a malformed token string, When validated, Then a failure result with "Invalid token" is returned.
    [Fact]
    public void MalformedToken_ReturnsFailure()
    {
        // Act
        var result = _validator.ValidateToken("not-a-valid-jwt-token");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid token");
    }

    public void Dispose() { }
}
