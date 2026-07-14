using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Payment.Application.Common.Exceptions;
using Payment.Application.Features.Payments.Commands.CancelPayment;
using Payment.Domain.Enums;
using Payment.UnitTests.Fixtures;

namespace Payment.UnitTests.Application;

// Tests for the CancelPaymentCommandHandler: successful cancellation, state restrictions, and authorization checks.
public class CancelPaymentCommandHandlerTests : IDisposable
{
    private readonly global::Payment.Infrastructure.Persistence.PaymentDbContext _context;
    private readonly Mock<ILogger<CancelPaymentCommandHandler>> _loggerMock = new();
    private readonly CancelPaymentCommandHandler _handler;

    public CancelPaymentCommandHandlerTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _handler = new CancelPaymentCommandHandler(_context, _loggerMock.Object);
    }

    // Given a pending payment belonging to the user, When the cancel command is handled, Then status transitions to Cancelled.
    [Fact]
    public async Task PendingPayment_CancelsSuccessfully()
    {
        // Arrange
        var payment = await TestDbContextFactory.CreatePendingPaymentAsync(_context);

        // Act
        var command = new CancelPaymentCommand(payment.Id, payment.UserId);
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.PaymentId.Should().Be(payment.Id);
        result.Status.Should().Be("cancelled");

        var updatedPayment = await _context.Payments.FindAsync(payment.Id);
        updatedPayment!.Status.Should().Be(PaymentStatus.Cancelled);
    }

    // Given a completed payment, When the cancel command is handled, Then a PaymentException is thrown.
    [Fact]
    public async Task NonPendingPayment_ThrowsPaymentException()
    {
        // Arrange
        var payment = await TestDbContextFactory.CreateCompletedPaymentAsync(_context);

        // Act
        var command = new CancelPaymentCommand(payment.Id, payment.UserId);
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Payment.Domain.Exceptions.PaymentException>()
            .WithMessage("*Cannot cancel payment*");
    }

    // Given a pending payment with a different UserId, When the cancel command is handled, Then a NotFoundException is thrown.
    [Fact]
    public async Task WrongUserId_ThrowsNotFoundException()
    {
        // Arrange
        var payment = await TestDbContextFactory.CreatePendingPaymentAsync(_context);

        // Act
        var command = new CancelPaymentCommand(payment.Id, Guid.NewGuid());
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // Given a non-existent payment id, When the cancel command is handled, Then a NotFoundException is thrown.
    [Fact]
    public async Task NotFoundPayment_ThrowsNotFoundException()
    {
        // Arrange & Act
        var command = new CancelPaymentCommand(Guid.NewGuid(), Guid.NewGuid());
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
