using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Payment.Infrastructure.PaymentGateway;

namespace Payment.UnitTests.Infrastructure;

// Tests for the FakePaymentGateway: credit card Luhn validation, expiry checks, Pix, and Boleto processing.
public class FakePaymentGatewayTests
{
    private readonly FakePaymentGateway _gateway;

    public FakePaymentGatewayTests()
    {
        var loggerMock = new Mock<ILogger<FakePaymentGateway>>();
        _gateway = new FakePaymentGateway(loggerMock.Object);
    }

    // Given a credit card number that passes the Luhn algorithm, When processed, Then a successful result is returned.
    [Theory]
    [InlineData("4111111111111111")]
    [InlineData("5500000000000004")]
    [InlineData("378282246310005")]
    public async Task ValidLuhnCard_ReturnsSuccess(string cardNumber)
    {
        // Act
        var result = await _gateway.ProcessCreditCardAsync(
            29.90m, cardNumber, "123",
            12, DateTime.UtcNow.Year + 1, "John Doe");

        // Assert
        result.GatewayPaymentId.Should().NotBeNullOrEmpty();
        result.Success.Should().BeTrue();
    }

    // Given a credit card number that fails the Luhn algorithm, When processed, Then an invalid card result is returned.
    [Fact]
    public async Task InvalidLuhnCard_ReturnsInvalid()
    {
        // Act
        var result = await _gateway.ProcessCreditCardAsync(
            29.90m, "1234567890123456", "123",
            12, DateTime.UtcNow.Year + 1, "John Doe");

        // Assert
        result.Success.Should().BeFalse();
        result.GatewayMessage.Should().Be("Invalid card number");
    }

    // Given a valid card number with an expired date, When processed, Then an expired card result is returned.
    [Fact]
    public async Task ExpiredCard_ReturnsExpired()
    {
        // Act
        var result = await _gateway.ProcessCreditCardAsync(
            29.90m, "4111111111111111", "123",
            1, DateTime.UtcNow.Year - 1, "John Doe");

        // Assert
        result.Success.Should().BeFalse();
        result.GatewayMessage.Should().Be("Card expired");
    }

    // Given a Pix payment amount, When processed, Then a successful Pix result with a pix code is returned.
    [Fact]
    public async Task PixPayment_ReturnsSuccess()
    {
        // Act
        var result = await _gateway.ProcessPixAsync(29.90m);

        // Assert
        result.Success.Should().BeTrue();
        result.GatewayPaymentId.Should().StartWith("pix_");
        result.RawResponse.Should().Contain("pix_code");
    }

    // Given a Boleto payment amount, When processed, Then a successful Boleto result with a nosso_numero is returned.
    [Fact]
    public async Task BoletoPayment_ReturnsSuccess()
    {
        // Act
        var result = await _gateway.ProcessBoletoAsync(99.90m);

        // Assert
        result.Success.Should().BeTrue();
        result.GatewayPaymentId.Should().StartWith("boleto_");
        result.RawResponse.Should().Contain("nosso_numero");
    }
}
