using FluentValidation;

namespace Payment.Application.Features.Payments.Commands.RefundPayment;

// Validates that PaymentId and UserId are provided in the RefundPaymentCommand.
public sealed class RefundPaymentCommandValidator : AbstractValidator<RefundPaymentCommand>
{
    public RefundPaymentCommandValidator()
    {
        RuleFor(x => x.PaymentId)
            .NotEmpty().WithMessage("PaymentId is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}
