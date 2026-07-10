using FluentValidation;

namespace Payment.Application.Features.Payments.Commands.ProcessPayment;

public sealed class ProcessPaymentCommandValidator
    : AbstractValidator<ProcessPaymentCommand>
{
    public ProcessPaymentCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.PlanType)
            .Must(p => p is "pro" or "enterprise")
            .WithMessage("Plan must be 'pro' or 'enterprise'");
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.IdempotencyKey).NotEmpty();
        RuleFor(x => x.PaymentMethod)
            .Must(m => m is "credit_card" or "pix" or "boleto");

        When(x => x.PaymentMethod == "credit_card", () =>
        {
            RuleFor(x => x.CardNumber).NotEmpty().Length(13, 19);
            RuleFor(x => x.CardCvv).NotEmpty().Length(3, 4);
            RuleFor(x => x.CardExpiryMonth).InclusiveBetween(1, 12);
            RuleFor(x => x.CardExpiryYear).GreaterThanOrEqualTo(DateTime.UtcNow.Year);
            RuleFor(x => x.CardHolderName).NotEmpty().MaximumLength(100);
        });
    }
}
