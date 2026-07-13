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

public class ProcessPaymentCommandHandlerTests : IDisposable
{
    private readonly global::Payment.Infrastructure.Persistence.PaymentDbContext _context;
    private readonly Mock<IPaymentGateway> _gatewayMock = new();
    private readonly Mock<IMessageBus> _busMock = new();
    private readonly Mock<ILogger<ProcessPaymentCommandHandler>> _loggerMock = new();
    private readonly ProcessPaymentCommandHandler _handler;

    public ProcessPaymentCommandHandlerTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _handler = new ProcessPaymentCommandHandler(
            _context,
            _gatewayMock.Object,
            _busMock.Object,
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

    [Fact]
    public async Task NewPayment_Success_CallsGatewayAndReturnsCompleted()
    {
        var command = ValidCommand("new-success-key");

        _gatewayMock.Setup(g => g.ProcessCreditCardAsync(
                It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new PaymentResult(true, "gw_123", "Approved", "{}"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Status.Should().Be("completed");
        result.ErrorMessage.Should().BeNull();

        var savedPayment = _context.Payments
            .First(p => p.IdempotencyKey == "new-success-key");
        savedPayment.Status.Should().Be(PaymentStatus.Completed);
        savedPayment.GatewayPaymentId.Should().Be("gw_123");

        _gatewayMock.Verify(g => g.ProcessCreditCardAsync(
            It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Once);
        _busMock.Verify(b => b.PublishAsync(
            It.IsAny<PaymentCompletedEvent>(),
            "payment.completed",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DuplicateIdempotency_ThrowsDuplicatePaymentException()
    {
        await TestDbContextFactory.CreateCompletedPaymentAsync(
            _context, idempotencyKey: "dup-key");

        var command = ValidCommand("dup-key");

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DuplicatePaymentException>()
            .WithMessage("*dup-key*");
    }

    [Fact]
    public async Task GatewayThrows_MarksFailedAndReturnsError()
    {
        var command = ValidCommand("gw-fail-key");

        _gatewayMock.Setup(g => g.ProcessCreditCardAsync(
                It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Gateway timeout"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Status.Should().Be("failed");
        result.ErrorMessage.Should().Be("Gateway timeout");

        var savedPayment = _context.Payments
            .First(p => p.IdempotencyKey == "gw-fail-key");
        savedPayment.Status.Should().Be(PaymentStatus.Failed);
    }

    [Fact]
    public async Task PixPayment_Success()
    {
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

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Status.Should().Be("completed");
        _gatewayMock.Verify(g => g.ProcessPixAsync(It.IsAny<decimal>()), Times.Once);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
