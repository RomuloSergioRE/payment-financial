using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Payment.Application.Common.Exceptions;
using Payment.Application.Features.Payments.Queries.GetPayment;
using Payment.UnitTests.Fixtures;

namespace Payment.UnitTests.Application;

// Tests for the GetPaymentQueryHandler: successful retrieval, not found, and authorization checks.
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

    // Given an existing payment owned by the user, When the query is handled, Then the payment data is returned.
    [Fact]
    public async Task PaymentExists_ReturnsResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var payment = await TestDbContextFactory.CreateCompletedPaymentAsync(
            _context, userId: userId);

        // Act
        var query = new GetPaymentQuery(payment.Id, userId);
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.PaymentId.Should().Be(payment.Id);
        result.UserId.Should().Be(userId);
        result.PlanType.Should().Be("Pro");
        result.Status.Should().Be("Completed");
    }

    // Given a non-existent payment id, When the query is handled, Then a NotFoundException is thrown.
    [Fact]
    public async Task PaymentNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var query = new GetPaymentQuery(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // Given a payment that belongs to a different user, When the query is handled, Then a NotFoundException is thrown.
    [Fact]
    public async Task WrongUserId_ThrowsNotFoundException()
    {
        // Arrange
        var payment = await TestDbContextFactory.CreateCompletedPaymentAsync(_context);

        // Act
        var query = new GetPaymentQuery(payment.Id, Guid.NewGuid());
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
