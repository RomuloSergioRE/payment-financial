using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Payment.Application.Common.Exceptions;
using Payment.Application.Features.Payments.Commands.ProcessPayment;
using Payment.Application.Features.Payments.Commands.CancelPayment;
using Payment.Domain.Enums;
using Payment.IntegrationTests.Fixtures;
using PaymentDbContext = global::Payment.Infrastructure.Persistence.PaymentDbContext;

namespace Payment.IntegrationTests;

// Integration tests for payment processing: end-to-end idempotency and cancellation flows.
public class ProcessPaymentIntegrationTests : IDisposable
{
    private readonly PaymentDbContext _context;

    public ProcessPaymentIntegrationTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
    }

    // Given a payment with an existing idempotency key in the database, When a new command with the same key is processed, Then a DuplicatePaymentException is thrown.
    [Fact]
    public async Task IdempotencyCheck_SameKeyReturnsDuplicateException()
    {
        // Arrange
        TestDbContextFactory.SeedData(_context);

        var handler = new ProcessPaymentCommandHandler(
            _context,
            Mock.Of<global::Payment.Application.Common.Interfaces.IPaymentGateway>(),
            Mock.Of<ILogger<ProcessPaymentCommandHandler>>());

        var command = new ProcessPaymentCommand(
            Guid.NewGuid(),
            "pro",
            29.90m,
            "BRL",
            "credit_card",
            "existing-idempotency-key",
            "4111111111111111",
            "123",
            12,
            DateTime.UtcNow.Year + 1,
            "John Doe");

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DuplicatePaymentException>();
    }

    // Given a seeded pending payment, When the cancel command is handled, Then the payment status transitions to Cancelled in the database.
    [Fact]
    public async Task CancelPendingPayment_UpdatesStatusToCancelled()
    {
        // Arrange
        TestDbContextFactory.SeedData(_context);
        var payment = _context.Payments.First();

        var handler = new CancelPaymentCommandHandler(
            _context,
            Mock.Of<ILogger<CancelPaymentCommandHandler>>());

        // Act
        var command = new CancelPaymentCommand(payment.Id, payment.UserId);
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be("cancelled");
        var updatedPayment = await _context.Payments.FindAsync(payment.Id);
        updatedPayment!.Status.Should().Be(PaymentStatus.Cancelled);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
