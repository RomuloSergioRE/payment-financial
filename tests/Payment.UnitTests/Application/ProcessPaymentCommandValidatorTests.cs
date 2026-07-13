using FluentAssertions;
using FluentValidation.TestHelper;
using Payment.Application.Features.Payments.Commands.ProcessPayment;

namespace Payment.UnitTests.Application;

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

    [Fact]
    public async Task ValidCommand_PassesValidation()
    {
        var result = await _validator.TestValidateAsync(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task EmptyUserId_Fails()
    {
        var cmd = ValidCommand() with { UserId = Guid.Empty };

        var result = await _validator.TestValidateAsync(cmd);

        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public async Task InvalidPlanType_Fails()
    {
        var cmd = ValidCommand(plan: "enterprise") with { PlanType = "basic" };

        var result = await _validator.TestValidateAsync(cmd);

        result.ShouldHaveValidationErrorFor(x => x.PlanType);
    }

    [Theory]
    [InlineData("pro")]
    [InlineData("enterprise")]
    public async Task ValidPlanType_Passes(string plan)
    {
        var cmd = ValidCommand(plan: plan);

        var result = await _validator.TestValidateAsync(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.PlanType);
    }

    [Fact]
    public async Task ZeroAmount_Fails()
    {
        var cmd = ValidCommand() with { Amount = 0 };

        var result = await _validator.TestValidateAsync(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public async Task NegativeAmount_Fails()
    {
        var cmd = ValidCommand() with { Amount = -10m };

        var result = await _validator.TestValidateAsync(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public async Task EmptyCurrency_Fails()
    {
        var cmd = ValidCommand() with { Currency = "" };

        var result = await _validator.TestValidateAsync(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public async Task LongCurrency_Fails()
    {
        var cmd = ValidCommand() with { Currency = "BRLX" };

        var result = await _validator.TestValidateAsync(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public async Task EmptyIdempotencyKey_Fails()
    {
        var cmd = ValidCommand() with { IdempotencyKey = "" };

        var result = await _validator.TestValidateAsync(cmd);

        result.ShouldHaveValidationErrorFor(x => x.IdempotencyKey);
    }

    [Theory]
    [InlineData("credit_card")]
    [InlineData("pix")]
    [InlineData("boleto")]
    public async Task ValidPaymentMethod_Passes(string method)
    {
        var cmd = ValidCommand(method: method);

        var result = await _validator.TestValidateAsync(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.PaymentMethod);
    }

    [Fact]
    public async Task InvalidPaymentMethod_Fails()
    {
        var cmd = ValidCommand() with { PaymentMethod = "crypto" };

        var result = await _validator.TestValidateAsync(cmd);

        result.ShouldHaveValidationErrorFor(x => x.PaymentMethod);
    }

    [Fact]
    public async Task CreditCard_WithoutCardNumber_Fails()
    {
        var cmd = ValidCommand() with { CardNumber = null };

        var result = await _validator.TestValidateAsync(cmd);

        result.ShouldHaveValidationErrorFor(x => x.CardNumber);
    }

    [Fact]
    public async Task CreditCard_WithoutCvv_Fails()
    {
        var cmd = ValidCommand() with { CardCvv = null };

        var result = await _validator.TestValidateAsync(cmd);

        result.ShouldHaveValidationErrorFor(x => x.CardCvv);
    }

    [Fact]
    public async Task CreditCard_WithoutHolderName_Fails()
    {
        var cmd = ValidCommand() with { CardHolderName = null };

        var result = await _validator.TestValidateAsync(cmd);

        result.ShouldHaveValidationErrorFor(x => x.CardHolderName);
    }

    [Fact]
    public async Task CreditCard_ExpiredYear_Fails()
    {
        var cmd = ValidCommand() with { CardExpiryYear = DateTime.UtcNow.Year - 1 };

        var result = await _validator.TestValidateAsync(cmd);

        result.ShouldHaveValidationErrorFor(x => x.CardExpiryYear);
    }

    [Fact]
    public async Task Pix_NoCardFieldsRequired()
    {
        var cmd = ValidCommand(method: "pix") with
        {
            CardNumber = null,
            CardCvv = null,
            CardExpiryMonth = null,
            CardExpiryYear = null,
            CardHolderName = null
        };

        var result = await _validator.TestValidateAsync(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.CardNumber);
        result.ShouldNotHaveValidationErrorFor(x => x.CardCvv);
    }
}
