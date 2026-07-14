using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Payment.Application.Common.Exceptions;
using Payment.Application.Common.Interfaces;
using Payment.Domain.Entities;
using Payment.Domain.Enums;

namespace Payment.Application.Features.Payments.Commands.CancelPayment;

// Handles the CancelPaymentCommand by validating ownership and status,
// then transitioning the payment to Cancelled state.
public sealed class CancelPaymentCommandHandler
    : IRequestHandler<CancelPaymentCommand, CancelPaymentResponse>
{
    private readonly IPaymentDbContext _context;
    private readonly ILogger<CancelPaymentCommandHandler> _logger;

    public CancelPaymentCommandHandler(
        IPaymentDbContext context,
        ILogger<CancelPaymentCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Cancels a payment: verifies ownership, checks status, and transitions to Cancelled.
    public async Task<CancelPaymentResponse> Handle(
        CancelPaymentCommand request,
        CancellationToken cancellationToken)
    {
        // PASSO 1: Buscar o pagamento pelo ID.
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken);

        if (payment is null)
        {
            _logger.LogWarning("Payment {PaymentId} not found", request.PaymentId);
            throw new NotFoundException("Payment", request.PaymentId);
        }

        // PASSO 2: Verificar propriedade — usuário só pode cancelar seus próprios pagamentos.
        if (payment.UserId != request.UserId)
        {
            _logger.LogWarning(
                "Unauthorized cancel attempt for payment {PaymentId}",
                request.PaymentId);
            throw new NotFoundException("Payment", request.PaymentId);
        }

        // PASSO 3: Verificar se o pagamento está pendente (único status cancelável).
        if (payment.Status != PaymentStatus.Pending)
        {
            _logger.LogWarning(
                "Cannot cancel payment {PaymentId} with status {Status}",
                request.PaymentId, payment.Status);
            throw new Domain.Exceptions.PaymentException(
                $"Cannot cancel payment with status '{payment.Status}'. Only pending payments can be cancelled.");
        }

        // PASSO 4: Transicionar para Cancelled e registrar log.
        payment.MarkCancelled();
        _context.PaymentLogs.Add(new PaymentLog(
            payment.Id, PaymentLog.EventTypes.Cancelled));

        _logger.LogInformation(
            "Payment {PaymentId} cancelled by user {UserId}",
            request.PaymentId, request.UserId);

        return new CancelPaymentResponse(
            payment.Id,
            payment.Status.ToString().ToLowerInvariant());
    }
}
