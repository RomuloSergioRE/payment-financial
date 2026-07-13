using FluentValidation;

namespace Payment.Application.Features.Payments.Commands.CancelPayment;

public sealed class CancelPaymentCommandValidator
    : AbstractValidator<CancelPaymentCommand>
{
    public CancelPaymentCommandValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
