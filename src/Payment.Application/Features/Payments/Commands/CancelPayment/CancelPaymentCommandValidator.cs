using FluentValidation;

namespace Payment.Application.Features.Payments.Commands.CancelPayment;

// Validates that PaymentId and UserId are provided in the CancelPaymentCommand.
public sealed class CancelPaymentCommandValidator
    : AbstractValidator<CancelPaymentCommand>
{
    public CancelPaymentCommandValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
