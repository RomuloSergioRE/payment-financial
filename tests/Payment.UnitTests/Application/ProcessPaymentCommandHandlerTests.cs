using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Payment.Application.Common.Exceptions;
using Payment.Application.Common.Interfaces;
using Payment.Application.Common.Models;
using Payment.Application.Features.Payments.Commands.ProcessPayment;
using Payment.Domain.Enums;
using Payment.UnitTests.Fixtures;
using PaymentEntity = global::Payment.Domain.Entities.Payment;

namespace Payment.UnitTests.Application;

// Tests for the ProcessPaymentCommandHandler: success, idempotency, gateway failures, and payment log creation.
public class ProcessPaymentCommandHandlerTests : IDisposable
{
    private readonly global::Payment.Infrastructure.Persistence.PaymentDbContext _context;
    private readonly Mock<IPaymentGateway> _gatewayMock = new();
    private readonly Mock<ILogger<ProcessPaymentCommandHandler>> _loggerMock = new();
    private readonly ProcessPaymentCommandHandler _handler;

    public ProcessPaymentCommandHandlerTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _handler = new ProcessPaymentCommandHandler(
            _context,
            _gatewayMock.Object,
            _loggerMock.Object);
    }

    private static ProcessPaymentCommand ValidCommand(string idempotencyKey = "test-key") => new(
        UserId: Guid.NewGuid(),
        PlanType: "pro",
        Amount: 29.90m,
        Currency: "BRL",
        PaymentMethod: "credit_card",
        IdempotencyKey: idempotencyKey,
        CardNumber: "4111111111111111",
        CardCvv: "123",
        CardExpiryMonth: 12,
        CardExpiryYear: DateTime.UtcNow.Year + 1,
        CardHolderName: "John Doe");

    // Given a valid credit card command with unique idempotency key, When processed, Then gateway is called and payment is saved as Completed.
    [Fact]
    public async Task NewPayment_Success_CallsGatewayAndReturnsCompleted()
    {
        // Arrange
        var command = ValidCommand("new-success-key");

        _gatewayMock.Setup(g => g.ProcessCreditCardAsync(
                It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new PaymentResult(true, "gw_123", "Approved", "{}"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be("completed");
        result.ErrorMessage.Should().BeNull();

        // Salva manualmente — o handler não chama SaveChanges (TransactionBehaviour cuida disso)
        await _context.SaveChangesAsync();

        var savedPayment = _context.Payments
            .First(p => p.IdempotencyKey == "new-success-key");
        savedPayment.Status.Should().Be(PaymentStatus.Completed);
        savedPayment.GatewayPaymentId.Should().Be("gw_123");

        _gatewayMock.Verify(g => g.ProcessCreditCardAsync(
            It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Once);
    }

    // Given a command with an idempotency key that already exists, When processed, Then a DuplicatePaymentException is thrown.
    [Fact]
    public async Task DuplicateIdempotency_ThrowsDuplicatePaymentException()
    {
        // Arrange
        await TestDbContextFactory.CreateCompletedPaymentAsync(
            _context, idempotencyKey: "dup-key");

        var command = ValidCommand("dup-key");

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DuplicatePaymentException>()
            .WithMessage("*dup-key*");
    }

    // Given a valid command and the gateway throws an exception, When processed, Then payment is marked as Failed and error message is returned.
    [Fact]
    public async Task GatewayThrows_MarksFailedAndReturnsError()
    {
        // Arrange
        var command = ValidCommand("gw-fail-key");

        _gatewayMock.Setup(g => g.ProcessCreditCardAsync(
                It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Gateway timeout"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be("failed");
        result.ErrorMessage.Should().Be("Gateway timeout");

        await _context.SaveChangesAsync();

        var savedPayment = _context.Payments
            .First(p => p.IdempotencyKey == "gw-fail-key");
        savedPayment.Status.Should().Be(PaymentStatus.Failed);
    }

    // Given a valid Pix payment command, When processed, Then the Pix gateway method is called and payment is Completed.
    [Fact]
    public async Task PixPayment_Success()
    {
        // Arrange
        var command = ValidCommand("pix-key") with
        {
            PaymentMethod = "pix",
            CardNumber = null,
            CardCvv = null,
            CardExpiryMonth = null,
            CardExpiryYear = null,
            CardHolderName = null
        };

        _gatewayMock.Setup(g => g.ProcessPixAsync(It.IsAny<decimal>()))
            .ReturnsAsync(new PaymentResult(true, "pix_123", "PIX completed", "{}"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be("completed");
        _gatewayMock.Verify(g => g.ProcessPixAsync(It.IsAny<decimal>()), Times.Once);
    }

    // Given a valid Boleto payment command, When processed, Then the Boleto gateway method is called and payment is Completed.
    [Fact]
    public async Task BoletoPayment_Success()
    {
        // Arrange
        var command = ValidCommand("boleto-key") with
        {
            PaymentMethod = "boleto",
            CardNumber = null,
            CardCvv = null,
            CardExpiryMonth = null,
            CardExpiryYear = null,
            CardHolderName = null
        };

        _gatewayMock.Setup(g => g.ProcessBoletoAsync(It.IsAny<decimal>()))
            .ReturnsAsync(new PaymentResult(true, "boleto_123", "Boleto generated", "{}"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be("completed");
        _gatewayMock.Verify(g => g.ProcessBoletoAsync(It.IsAny<decimal>()), Times.Once);
    }

    // Given a valid command and the gateway returns a failure result, When processed, Then payment is marked as Failed with the gateway error message.
    [Fact]
    public async Task GatewayReturnsFailure_MarksFailedAndReturnsError()
    {
        // Arrange
        var command = ValidCommand("gw-fail-result-key");

        _gatewayMock.Setup(g => g.ProcessCreditCardAsync(
                It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new PaymentResult(false, "", "Card declined by issuer", null));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be("failed");
        result.ErrorMessage.Should().Be("Card declined by issuer");

        await _context.SaveChangesAsync();

        var savedPayment = _context.Payments
            .First(p => p.IdempotencyKey == "gw-fail-result-key");
        savedPayment.Status.Should().Be(PaymentStatus.Failed);
    }

    // Given a successful payment processing, When completed, Then payment log entries are created for each state transition.
    [Fact]
    public async Task SuccessfulPayment_CreatesPaymentLogEntries()
    {
        // Arrange
        var command = ValidCommand("log-verify-key");

        _gatewayMock.Setup(g => g.ProcessCreditCardAsync(
                It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new PaymentResult(true, "gw_log", "Approved", "{}"));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _context.SaveChangesAsync();

        var payment = _context.Payments
            .First(p => p.IdempotencyKey == "log-verify-key");
        var logs = _context.PaymentLogs
            .Where(l => l.PaymentId == payment.Id)
            .Select(l => l.EventType)
            .ToList();

        logs.Should().Contain("payment.created");
        logs.Should().Contain("payment.processing");
        logs.Should().Contain("payment.completed");
        logs.Should().NotContain("payment.failed");
    }

    // Given a command where the gateway throws, When processed, Then no outbox messages are created.
    [Fact]
    public async Task FailedPayment_DoesNotCreateOutboxEntry()
    {
        // Arrange
        var command = ValidCommand("no-event-key");

        _gatewayMock.Setup(g => g.ProcessCreditCardAsync(
                It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Gateway error"));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _context.OutboxMessages.Should().BeEmpty();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
