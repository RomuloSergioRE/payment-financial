using FluentAssertions;
using FluentValidation.TestHelper;
using Payment.Application.Features.Payments.Commands.ProcessPayment;

namespace Payment.UnitTests.Application;

// Tests for the ProcessPaymentCommandValidator: field presence, format, and conditional validation rules.
public class ProcessPaymentCommandValidatorTests
{
    private readonly ProcessPaymentCommandValidator _validator = new();

    private static ProcessPaymentCommand ValidCommand(
        string method = "credit_card",
        string plan = "pro") => new(
        UserId: Guid.NewGuid(),
        PlanType: plan,
        Amount: 29.90m,
        Currency: "BRL",
        PaymentMethod: method,
        IdempotencyKey: Guid.NewGuid().ToString(),
        CardNumber: "4111111111111111",
        CardCvv: "123",
        CardExpiryMonth: 12,
        CardExpiryYear: DateTime.UtcNow.Year + 1,
        CardHolderName: "John Doe");

    // Given a fully valid command, When validated, Then no validation errors are returned.
    [Fact]
    public async Task ValidCommand_PassesValidation()
    {
        // Act
        var result = await _validator.TestValidateAsync(ValidCommand());

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // Given a command with an empty UserId, When validated, Then a validation error is returned for UserId.
    [Fact]
    public async Task EmptyUserId_Fails()
    {
        // Arrange
        var cmd = ValidCommand() with { UserId = Guid.Empty };

        // Act
        var result = await _validator.TestValidateAsync(cmd);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    // Given a command with an invalid PlanType, When validated, Then a validation error is returned for PlanType.
    [Fact]
    public async Task InvalidPlanType_Fails()
    {
        // Arrange
        var cmd = ValidCommand(plan: "enterprise") with { PlanType = "basic" };

        // Act
        var result = await _validator.TestValidateAsync(cmd);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PlanType);
    }

    // Given a command with valid plan types (pro, enterprise), When validated, Then no PlanType errors are returned.
    [Theory]
    [InlineData("pro")]
    [InlineData("enterprise")]
    public async Task ValidPlanType_Passes(string plan)
    {
        // Arrange
        var cmd = ValidCommand(plan: plan);

        // Act
        var result = await _validator.TestValidateAsync(cmd);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PlanType);
    }

    // Given a command with zero amount, When validated, Then a validation error is returned for Amount.
    [Fact]
    public async Task ZeroAmount_Fails()
    {
        // Arrange
        var cmd = ValidCommand() with { Amount = 0 };

        // Act
        var result = await _validator.TestValidateAsync(cmd);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    // Given a command with negative amount, When validated, Then a validation error is returned for Amount.
    [Fact]
    public async Task NegativeAmount_Fails()
    {
        // Arrange
        var cmd = ValidCommand() with { Amount = -10m };

        // Act
        var result = await _validator.TestValidateAsync(cmd);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    // Given a command with empty currency, When validated, Then a validation error is returned for Currency.
    [Fact]
    public async Task EmptyCurrency_Fails()
    {
        // Arrange
        var cmd = ValidCommand() with { Currency = "" };

        // Act
        var result = await _validator.TestValidateAsync(cmd);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    // Given a command with currency longer than 3 characters, When validated, Then a validation error is returned for Currency.
    [Fact]
    public async Task LongCurrency_Fails()
    {
        // Arrange
        var cmd = ValidCommand() with { Currency = "BRLX" };

        // Act
        var result = await _validator.TestValidateAsync(cmd);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    // Given a command with an empty idempotency key, When validated, Then a validation error is returned for IdempotencyKey.
    [Fact]
    public async Task EmptyIdempotencyKey_Fails()
    {
        // Arrange
        var cmd = ValidCommand() with { IdempotencyKey = "" };

        // Act
        var result = await _validator.TestValidateAsync(cmd);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.IdempotencyKey);
    }

    // Given a command with valid payment methods (credit_card, pix, boleto), When validated, Then no PaymentMethod errors are returned.
    [Theory]
    [InlineData("credit_card")]
    [InlineData("pix")]
    [InlineData("boleto")]
    public async Task ValidPaymentMethod_Passes(string method)
    {
        // Arrange
        var cmd = ValidCommand(method: method);

        // Act
        var result = await _validator.TestValidateAsync(cmd);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PaymentMethod);
    }

    // Given a command with an unsupported payment method, When validated, Then a validation error is returned for PaymentMethod.
    [Fact]
    public async Task InvalidPaymentMethod_Fails()
    {
        // Arrange
        var cmd = ValidCommand() with { PaymentMethod = "crypto" };

        // Act
        var result = await _validator.TestValidateAsync(cmd);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PaymentMethod);
    }

    // Given a credit card payment without a card number, When validated, Then a validation error is returned for CardNumber.
    [Fact]
    public async Task CreditCard_WithoutCardNumber_Fails()
    {
        // Arrange
        var cmd = ValidCommand() with { CardNumber = null };

        // Act
        var result = await _validator.TestValidateAsync(cmd);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CardNumber);
    }

    // Given a credit card payment without CVV, When validated, Then a validation error is returned for CardCvv.
    [Fact]
    public async Task CreditCard_WithoutCvv_Fails()
    {
        // Arrange
        var cmd = ValidCommand() with { CardCvv = null };

        // Act
        var result = await _validator.TestValidateAsync(cmd);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CardCvv);
    }

    // Given a credit card payment without holder name, When validated, Then a validation error is returned for CardHolderName.
    [Fact]
    public async Task CreditCard_WithoutHolderName_Fails()
    {
        // Arrange
        var cmd = ValidCommand() with { CardHolderName = null };

        // Act
        var result = await _validator.TestValidateAsync(cmd);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CardHolderName);
    }

    // Given a credit card payment with an expired year, When validated, Then a validation error is returned for CardExpiryYear.
    [Fact]
    public async Task CreditCard_ExpiredYear_Fails()
    {
        // Arrange
        var cmd = ValidCommand() with { CardExpiryYear = DateTime.UtcNow.Year - 1 };

        // Act
        var result = await _validator.TestValidateAsync(cmd);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CardExpiryYear);
    }

    // Given a Pix payment with all card fields null, When validated, Then no card-related validation errors are returned.
    [Fact]
    public async Task Pix_NoCardFieldsRequired()
    {
        // Arrange
        var cmd = ValidCommand(method: "pix") with
        {
            CardNumber = null,
            CardCvv = null,
            CardExpiryMonth = null,
            CardExpiryYear = null,
            CardHolderName = null
        };

        // Act
        var result = await _validator.TestValidateAsync(cmd);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CardNumber);
        result.ShouldNotHaveValidationErrorFor(x => x.CardCvv);
    }
}
