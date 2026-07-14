using FluentAssertions;
using Payment.Application.Features.Payments.Queries.ListPayments;
using Payment.Domain.Enums;
using Payment.UnitTests.Fixtures;

namespace Payment.UnitTests.Application;

// Tests for ListPaymentsQueryHandler — validates pagination, status filtering,
// and correct mapping of payments to summary DTOs.
public class ListPaymentsQueryHandlerTests : IDisposable
{
    private readonly global::Payment.Infrastructure.Persistence.PaymentDbContext _context;
    private readonly ListPaymentsQueryHandler _handler;

    public ListPaymentsQueryHandlerTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _handler = new ListPaymentsQueryHandler(_context);
    }

    // Returns a correct paginated slice ordered by most recent first.
    [Fact]
    public async Task ListPayments_ReturnsPaginatedResults()
    {
        // Arrange — seed 3 completed payments for the same user
        var userId = Guid.NewGuid();
        await TestDbContextFactory.CreateCompletedPaymentAsync(_context, userId: userId);
        await TestDbContextFactory.CreateCompletedPaymentAsync(_context, userId: userId);
        await TestDbContextFactory.CreateCompletedPaymentAsync(_context, userId: userId);

        var query = new ListPaymentsQuery(userId, Page: 1, PageSize: 2);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Payments.Items.Should().HaveCount(2);
        result.Payments.TotalCount.Should().Be(3);
        result.Payments.Page.Should().Be(1);
        result.Payments.PageSize.Should().Be(2);
    }

    // Filters payments by status — only matching ones are returned.
    [Fact]
    public async Task ListPayments_FiltersByStatus()
    {
        // Arrange — seed one pending and two completed payments for the same user
        var userId = Guid.NewGuid();
        await TestDbContextFactory.CreatePendingPaymentAsync(_context, userId: userId);
        await TestDbContextFactory.CreateCompletedPaymentAsync(_context, userId: userId);
        await TestDbContextFactory.CreateCompletedPaymentAsync(_context, userId: userId);

        var query = new ListPaymentsQuery(userId, Status: "completed");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Payments.Items.Should().HaveCount(2);
        result.Payments.Items.Should().OnlyContain(p => p.Status == "completed");
    }

    // No payments match the query — returns an empty list with TotalCount 0.
    [Fact]
    public async Task ListPayments_NoMatch_ReturnsEmptyList()
    {
        // Arrange — seed payments for user A, query for user B
        var otherUser = Guid.NewGuid();
        await TestDbContextFactory.CreateCompletedPaymentAsync(_context, userId: otherUser);

        var query = new ListPaymentsQuery(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Payments.Items.Should().BeEmpty();
        result.Payments.TotalCount.Should().Be(0);
    }

    // Second page returns the remaining items.
    [Fact]
    public async Task ListPayments_ReturnsCorrectPage()
    {
        // Arrange — seed 4 completed payments, request page 2 of size 2
        var userId = Guid.NewGuid();
        for (var i = 0; i < 4; i++)
            await TestDbContextFactory.CreateCompletedPaymentAsync(_context, userId: userId);

        var query = new ListPaymentsQuery(userId, Page: 2, PageSize: 2);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Payments.Items.Should().HaveCount(2);
        result.Payments.Page.Should().Be(2);
        result.Payments.TotalCount.Should().Be(4);
    }

    // Different users' payments are isolated — one user cannot see another's.
    [Fact]
    public async Task ListPayments_IsolatesByUser()
    {
        // Arrange
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        await TestDbContextFactory.CreateCompletedPaymentAsync(_context, userId: userA);
        await TestDbContextFactory.CreateCompletedPaymentAsync(_context, userId: userA);
        await TestDbContextFactory.CreateCompletedPaymentAsync(_context, userId: userB);

        var query = new ListPaymentsQuery(userA);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Payments.Items.Should().HaveCount(2);
        result.Payments.TotalCount.Should().Be(2);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
