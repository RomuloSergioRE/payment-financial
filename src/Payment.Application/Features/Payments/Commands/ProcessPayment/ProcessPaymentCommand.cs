using MediatR;
using Payment.Application.Common.Behaviours;

namespace Payment.Application.Features.Payments.Commands.ProcessPayment;

public sealed record ProcessPaymentCommand(
    Guid UserId,
    string PlanType,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    string IdempotencyKey,
    string? CardNumber,
    string? CardCvv,
    int? CardExpiryMonth,
    int? CardExpiryYear,
    string? CardHolderName
) : IRequest<ProcessPaymentResponse>, IPublishableRequest;
