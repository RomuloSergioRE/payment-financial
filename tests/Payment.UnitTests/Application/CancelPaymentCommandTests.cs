using FluentValidation.TestHelper;
using Payment.Application.Features.Payments.Commands.CancelPayment;

namespace Payment.UnitTests.Application;

public class CancelPaymentCommandTests
{
    [Fact]
    public async Task ValidCommand_PassesValidation()
    {
        var validator = new CancelPaymentCommandValidator();
        var cmd = new CancelPaymentCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = await validator.TestValidateAsync(cmd);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task EmptyPaymentId_Fails()
    {
        var validator = new CancelPaymentCommandValidator();
        var cmd = new CancelPaymentCommand(Guid.Empty, Guid.NewGuid());

        var result = await validator.TestValidateAsync(cmd);

        result.ShouldHaveValidationErrorFor(x => x.PaymentId);
    }

    [Fact]
    public async Task EmptyUserId_Fails()
    {
        var validator = new CancelPaymentCommandValidator();
        var cmd = new CancelPaymentCommand(Guid.NewGuid(), Guid.Empty);

        var result = await validator.TestValidateAsync(cmd);

        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}
