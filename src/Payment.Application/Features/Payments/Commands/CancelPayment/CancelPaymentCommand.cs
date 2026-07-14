using MediatR;
using Payment.Application.Common.Interfaces;

namespace Payment.Application.Features.Payments.Commands.CancelPayment;

// Command to cancel a pending payment.
// Implements ITransactionalRequest (executes within a transaction).
public sealed record CancelPaymentCommand(
    Guid PaymentId,
    Guid UserId) : IRequest<CancelPaymentResponse>, ITransactionalRequest;
