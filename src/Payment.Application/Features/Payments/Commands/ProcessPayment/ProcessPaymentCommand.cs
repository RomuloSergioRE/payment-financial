using MediatR;
using Payment.Application.Common.Interfaces;

namespace Payment.Application.Features.Payments.Commands.ProcessPayment;

// Command to process a new payment.
// Implements IPublishableRequest (outbox message) and ITransactionalRequest (executes within a transaction).
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
) : IRequest<ProcessPaymentResponse>, IPublishableRequest, ITransactionalRequest;
