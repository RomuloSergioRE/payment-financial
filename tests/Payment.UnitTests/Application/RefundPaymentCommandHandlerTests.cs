using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Payment.Application.Common.Exceptions;
using Payment.Application.Common.Interfaces;
using Payment.Application.Common.Models;
using Payment.Application.Features.Payments.Commands.RefundPayment;
using Payment.Domain.Enums;
using Payment.Domain.Exceptions;
using Payment.UnitTests.Fixtures;
using PaymentEntity = global::Payment.Domain.Entities.Payment;

namespace Payment.UnitTests.Application;

// Tests for RefundPaymentCommandHandler — validates ownership, gateway interaction,
// and correct payment state transitions during the refund flow.
public class RefundPaymentCommandHandlerTests : IDisposable
{
    private readonly global::Payment.Infrastructure.Persistence.PaymentDbContext _context;
    private readonly Mock<IPaymentGateway> _gatewayMock = new();
    private readonly Mock<ILogger<RefundPaymentCommandHandler>> _loggerMock = new();
    private readonly RefundPaymentCommandHandler _handler;

    public RefundPaymentCommandHandlerTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _handler = new RefundPaymentCommandHandler(
            _context,
            _gatewayMock.Object,
            _loggerMock.Object);
    }

    // Happy path: gateway approves refund, payment transitions to Refunded.
    [Fact]
    public async Task RefundSuccess_GatewayApproves_PaymentMarkedRefunded()
    {
        // Arrange — seed a completed payment owned by a known user
        var userId = Guid.NewGuid();
        var payment = await TestDbContextFactory.CreateCompletedPaymentAsync(
            _context, userId: userId);

        var command = new RefundPaymentCommand(payment.Id, userId, "Changed my mind");

        _gatewayMock
            .Setup(g => g.RefundAsync(payment.Amount.Amount, payment.GatewayPaymentId!))
            .ReturnsAsync(new PaymentResult(true, "ref_123", "Refund approved", "{}"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert — response
        result.Status.Should().Be("refunded");
        result.PaymentId.Should().Be(payment.Id);
        result.ErrorMessage.Should().BeNull();

        // Assert — entity persisted as Refunded
        var saved = await _context.Payments.FindAsync(payment.Id);
        saved!.Status.Should().Be(PaymentStatus.Refunded);
        saved.RefundedAt.Should().NotBeNull();

        // Assert — gateway called exactly once
        _gatewayMock.Verify(
            g => g.RefundAsync(payment.Amount.Amount, payment.GatewayPaymentId!),
            Times.Once);
    }

    // Gateway rejects the refund — payment stays Completed, response reports failure.
    [Fact]
    public async Task RefundFailure_GatewayRejects_PaymentStaysCompleted()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var payment = await TestDbContextFactory.CreateCompletedPaymentAsync(
            _context, userId: userId);

        var command = new RefundPaymentCommand(payment.Id, userId);

        _gatewayMock
            .Setup(g => g.RefundAsync(It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(new PaymentResult(false, "", "Refund window expired", null));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert — response
        result.Status.Should().Be("failed");
        result.ErrorMessage.Should().Be("Refund window expired");

        // Assert — payment unchanged
        var saved = await _context.Payments.FindAsync(payment.Id);
        saved!.Status.Should().Be(PaymentStatus.Completed);

        // Assert — no Refunded log entry
        _context.PaymentLogs
            .Any(l => l.PaymentId == payment.Id && l.EventType == "payment.refunded")
            .Should().BeFalse();
    }

    // Gateway throws an exception — payment stays Completed, handler catches it.
    [Fact]
    public async Task RefundGatewayThrows_PaymentStaysCompleted()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var payment = await TestDbContextFactory.CreateCompletedPaymentAsync(
            _context, userId: userId);

        var command = new RefundPaymentCommand(payment.Id, userId);

        _gatewayMock
            .Setup(g => g.RefundAsync(It.IsAny<decimal>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Gateway connection timed out"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert — response
        result.Status.Should().Be("failed");
        result.ErrorMessage.Should().Be("Gateway connection timed out");

        // Assert — payment unchanged
        var saved = await _context.Payments.FindAsync(payment.Id);
        saved!.Status.Should().Be(PaymentStatus.Completed);
    }

    // Payment does not exist in the database — handler throws NotFoundException.
    [Fact]
    public async Task RefundPaymentNotFound_ThrowsNotFoundException()
    {
        // Arrange — non-existent payment id
        var command = new RefundPaymentCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // Requester is not the payment owner — handler throws PaymentException.
    [Fact]
    public async Task RefundWrongUser_ThrowsPaymentException()
    {
        // Arrange — payment belongs to user A, refund requested by user B
        var owner = Guid.NewGuid();
        var payment = await TestDbContextFactory.CreateCompletedPaymentAsync(
            _context, userId: owner);

        var command = new RefundPaymentCommand(payment.Id, Guid.NewGuid());

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PaymentException>()
            .WithMessage("*access*");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
