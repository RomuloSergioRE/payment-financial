using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Payment.Application.Common.Exceptions;
using Payment.Application.Features.Payments.Commands.CancelPayment;
using Payment.Domain.Enums;
using Payment.UnitTests.Fixtures;

namespace Payment.UnitTests.Application;

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

    [Fact]
    public async Task PendingPayment_CancelsSuccessfully()
    {
        var payment = await TestDbContextFactory.CreatePendingPaymentAsync(_context);

        var command = new CancelPaymentCommand(payment.Id, payment.UserId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.PaymentId.Should().Be(payment.Id);
        result.Status.Should().Be("cancelled");

        var updatedPayment = await _context.Payments.FindAsync(payment.Id);
        updatedPayment!.Status.Should().Be(PaymentStatus.Cancelled);
    }

    [Fact]
    public async Task NonPendingPayment_ThrowsPaymentException()
    {
        var payment = await TestDbContextFactory.CreateCompletedPaymentAsync(_context);

        var command = new CancelPaymentCommand(payment.Id, payment.UserId);

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<Payment.Domain.Exceptions.PaymentException>()
            .WithMessage("*Cannot cancel payment*");
    }

    [Fact]
    public async Task WrongUserId_ThrowsNotFoundException()
    {
        var payment = await TestDbContextFactory.CreatePendingPaymentAsync(_context);

        var command = new CancelPaymentCommand(payment.Id, Guid.NewGuid());

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task NotFoundPayment_ThrowsNotFoundException()
    {
        var command = new CancelPaymentCommand(Guid.NewGuid(), Guid.NewGuid());

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
