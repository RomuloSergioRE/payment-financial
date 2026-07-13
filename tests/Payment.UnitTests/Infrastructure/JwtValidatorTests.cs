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

    [Fact]
    public void ValidToken_ReturnsSuccess()
    {
        var token = GenerateValidToken(
            userId: Guid.NewGuid(), role: "admin", plan: "enterprise");

        var result = _validator.ValidateToken(token);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Role.Should().Be("admin");
        result.Value.Plan.Should().Be("enterprise");
    }

    [Fact]
    public void ExpiredToken_ReturnsFailure()
    {
        var token = GenerateValidToken(
            expiry: DateTime.UtcNow.AddMinutes(-10));

        var result = _validator.ValidateToken(token);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Token expired");
    }

    [Fact]
    public void InvalidSignature_ReturnsFailure()
    {
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

        var result = _validator.ValidateToken(tokenString);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid token");
    }

    [Fact]
    public void MissingUserId_ReturnsFailure()
    {
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

        var result = _validator.ValidateToken(tokenString);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Missing required claims");
    }

    [Fact]
    public void MissingRole_ReturnsFailure()
    {
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

        var result = _validator.ValidateToken(tokenString);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Missing required claims");
    }

    [Fact]
    public void WrongIssuer_ReturnsFailure()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "wrong-issuer",
            audience: "zenyfin-payment",
            claims: new[] { new Claim("userId", Guid.NewGuid().ToString()) },
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var result = _validator.ValidateToken(tokenString);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid token");
    }

    [Fact]
    public void MalformedToken_ReturnsFailure()
    {
        var result = _validator.ValidateToken("not-a-valid-jwt-token");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid token");
    }

    public void Dispose() { }
}
