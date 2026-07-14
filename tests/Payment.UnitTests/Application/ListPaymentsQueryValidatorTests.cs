using FluentAssertions;
using Payment.Application.Features.Payments.Queries.ListPayments;

namespace Payment.UnitTests.Application;

// Tests for ListPaymentsQueryValidator — validates FluentValidation rules
// on pagination bounds, required fields, and status filter values.
public class ListPaymentsQueryValidatorTests
{
    private readonly ListPaymentsQueryValidator _validator = new();

    // A fully valid query passes all rules.
    [Fact]
    public void ValidQuery_PassesValidation()
    {
        // Arrange
        var query = new ListPaymentsQuery(Guid.NewGuid(), Page: 1, PageSize: 10);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    // Page less than 1 is rejected.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidPage_FailsValidation(int page)
    {
        // Arrange
        var query = new ListPaymentsQuery(Guid.NewGuid(), Page: page);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ListPaymentsQuery.Page));
    }

    // PageSize below 1 or above 100 is rejected.
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    [InlineData(200)]
    public void InvalidPageSize_FailsValidation(int pageSize)
    {
        // Arrange
        var query = new ListPaymentsQuery(Guid.NewGuid(), PageSize: pageSize);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ListPaymentsQuery.PageSize));
    }

    // PageSize at the boundary values (1 and 100) are accepted.
    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void BoundaryPageSize_PassesValidation(int pageSize)
    {
        // Arrange
        var query = new ListPaymentsQuery(Guid.NewGuid(), PageSize: pageSize);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    // An unrecognized status value is rejected.
    [Theory]
    [InlineData("invalid_status")]
    [InlineData("pending_processing")]
    [InlineData("unknown")]
    public void InvalidStatus_FailsValidation(string status)
    {
        // Arrange
        var query = new ListPaymentsQuery(Guid.NewGuid(), Status: status);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ListPaymentsQuery.Status));
    }

    // Valid lowercase status values pass validation.
    [Theory]
    [InlineData("pending")]
    [InlineData("processing")]
    [InlineData("completed")]
    [InlineData("failed")]
    [InlineData("refunded")]
    [InlineData("cancelled")]
    public void ValidStatus_PassesValidation(string status)
    {
        // Arrange
        var query = new ListPaymentsQuery(Guid.NewGuid(), Status: status);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    // Null status is allowed (no filter).
    [Fact]
    public void NullStatus_PassesValidation()
    {
        // Arrange
        var query = new ListPaymentsQuery(Guid.NewGuid(), Status: null);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    // Empty UserId is rejected.
    [Fact]
    public void EmptyUserId_FailsValidation()
    {
        // Arrange
        var query = new ListPaymentsQuery(Guid.Empty);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ListPaymentsQuery.UserId));
    }
}
