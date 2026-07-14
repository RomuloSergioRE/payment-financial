using MediatR;
using Payment.Application.Common.Interfaces;

namespace Payment.Application.Features.Payments.Commands.RefundPayment;

// Command to refund a completed payment.
// Implements ITransactionalRequest (executes within a transaction).
public sealed record RefundPaymentCommand(
    Guid PaymentId,
    Guid UserId,
    string? Reason = null) : IRequest<RefundPaymentResponse>, ITransactionalRequest;
