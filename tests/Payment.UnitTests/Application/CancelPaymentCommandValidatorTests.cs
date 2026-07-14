using FluentValidation.TestHelper;
using Payment.Application.Features.Payments.Commands.CancelPayment;

namespace Payment.UnitTests.Application;

// Tests for the CancelPaymentCommandValidator: ensures PaymentId and UserId are not empty.
public class CancelPaymentCommandValidatorTests
{
    // Given a valid command with both PaymentId and UserId set, When validated, Then no validation errors are returned.
    [Fact]
    public async Task ValidCommand_PassesValidation()
    {
        // Arrange
        var validator = new CancelPaymentCommandValidator();
        var cmd = new CancelPaymentCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await validator.TestValidateAsync(cmd);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // Given a command with an empty PaymentId, When validated, Then a validation error is returned for PaymentId.
    [Fact]
    public async Task EmptyPaymentId_Fails()
    {
        // Arrange
        var validator = new CancelPaymentCommandValidator();
        var cmd = new CancelPaymentCommand(Guid.Empty, Guid.NewGuid());

        // Act
        var result = await validator.TestValidateAsync(cmd);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PaymentId);
    }

    // Given a command with an empty UserId, When validated, Then a validation error is returned for UserId.
    [Fact]
    public async Task EmptyUserId_Fails()
    {
        // Arrange
        var validator = new CancelPaymentCommandValidator();
        var cmd = new CancelPaymentCommand(Guid.NewGuid(), Guid.Empty);

        // Act
        var result = await validator.TestValidateAsync(cmd);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}
