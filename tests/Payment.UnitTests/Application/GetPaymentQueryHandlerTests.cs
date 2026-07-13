using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Payment.Application.Common.Exceptions;
using Payment.Application.Features.Payments.Queries.GetPayment;
using Payment.UnitTests.Fixtures;

namespace Payment.UnitTests.Application;

public class GetPaymentQueryHandlerTests : IDisposable
{
    private readonly global::Payment.Infrastructure.Persistence.PaymentDbContext _context;
    private readonly Mock<ILogger<GetPaymentQueryHandler>> _loggerMock = new();
    private readonly GetPaymentQueryHandler _handler;

    public GetPaymentQueryHandlerTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _handler = new GetPaymentQueryHandler(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task PaymentExists_ReturnsResult()
    {
        var userId = Guid.NewGuid();
        var payment = await TestDbContextFactory.CreateCompletedPaymentAsync(
            _context, userId: userId);

        var query = new GetPaymentQuery(payment.Id, userId);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.PaymentId.Should().Be(payment.Id);
        result.UserId.Should().Be(userId);
        result.PlanType.Should().Be("Pro");
        result.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task PaymentNotFound_ThrowsNotFoundException()
    {
        var query = new GetPaymentQuery(Guid.NewGuid(), Guid.NewGuid());

        var act = async () => await _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task WrongUserId_ThrowsNotFoundException()
    {
        var payment = await TestDbContextFactory.CreateCompletedPaymentAsync(_context);

        var query = new GetPaymentQuery(payment.Id, Guid.NewGuid());

        var act = async () => await _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
