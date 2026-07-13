using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Payment.Infrastructure.PaymentGateway;

namespace Payment.UnitTests.Infrastructure;

public class FakePaymentGatewayTests
{
    private readonly FakePaymentGateway _gateway;

    public FakePaymentGatewayTests()
    {
        var loggerMock = new Mock<ILogger<FakePaymentGateway>>();
        _gateway = new FakePaymentGateway(loggerMock.Object);
    }

    [Theory]
    [InlineData("4111111111111111")]
    [InlineData("5500000000000004")]
    [InlineData("378282246310005")]
    public async Task ValidLuhnCard_ReturnsSuccess(string cardNumber)
    {
        var result = await _gateway.ProcessCreditCardAsync(
            29.90m, cardNumber, "123",
            12, DateTime.UtcNow.Year + 1, "John Doe");

        result.GatewayPaymentId.Should().NotBeNullOrEmpty();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task InvalidLuhnCard_ReturnsInvalid()
    {
        var result = await _gateway.ProcessCreditCardAsync(
            29.90m, "1234567890123456", "123",
            12, DateTime.UtcNow.Year + 1, "John Doe");

        result.Success.Should().BeFalse();
        result.GatewayMessage.Should().Be("Invalid card number");
    }

    [Fact]
    public async Task ExpiredCard_ReturnsExpired()
    {
        var result = await _gateway.ProcessCreditCardAsync(
            29.90m, "4111111111111111", "123",
            1, DateTime.UtcNow.Year - 1, "John Doe");

        result.Success.Should().BeFalse();
        result.GatewayMessage.Should().Be("Card expired");
    }

    [Fact]
    public async Task PixPayment_ReturnsSuccess()
    {
        var result = await _gateway.ProcessPixAsync(29.90m);

        result.Success.Should().BeTrue();
        result.GatewayPaymentId.Should().StartWith("pix_");
        result.RawResponse.Should().Contain("pix_code");
    }

    [Fact]
    public async Task BoletoPayment_ReturnsSuccess()
    {
        var result = await _gateway.ProcessBoletoAsync(99.90m);

        result.Success.Should().BeTrue();
        result.GatewayPaymentId.Should().StartWith("boleto_");
        result.RawResponse.Should().Contain("nosso_numero");
    }
}
