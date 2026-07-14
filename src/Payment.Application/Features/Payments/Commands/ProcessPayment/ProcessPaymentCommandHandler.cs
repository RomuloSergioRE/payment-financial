using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Payment.Application.Common.Exceptions;
using Payment.Application.Common.Interfaces;
using Payment.Application.Common.Models;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.Events;
using Payment.Domain.ValueObjects;
using PaymentEntity = Payment.Domain.Entities.Payment;

namespace Payment.Application.Features.Payments.Commands.ProcessPayment;

// Handles the ProcessPaymentCommand by validating idempotency, creating a payment entity,
// processing through the gateway, and dispatching domain events on completion or failure.
public sealed class ProcessPaymentCommandHandler
    : IRequestHandler<ProcessPaymentCommand, ProcessPaymentResponse>
{
    private readonly IPaymentDbContext _context;
    private readonly IPaymentGateway _gateway;
    private readonly ILogger<ProcessPaymentCommandHandler> _logger;

    public ProcessPaymentCommandHandler(
        IPaymentDbContext context,
        IPaymentGateway gateway,
        ILogger<ProcessPaymentCommandHandler> logger)
    {
        _context = context;
        _gateway = gateway;
        _logger = logger;
    }

    // Processes a payment: checks idempotency, creates entity, calls gateway, and raises domain events.
    public async Task<ProcessPaymentResponse> Handle(
        ProcessPaymentCommand command,
        CancellationToken cancellationToken)
    {
        // PASSO 1: Verificar idempotência — rejeitar se já existir pagamento com a mesma chave.
        var existing = await _context.Payments
            .FirstOrDefaultAsync(p => p.IdempotencyKey == command.IdempotencyKey,
                                 cancellationToken);

        if (existing is not null)
        {
            _logger.LogWarning(
                "Duplicate payment attempt. IdempotencyKey: {Key}, ExistingStatus: {Status}",
                command.IdempotencyKey, existing.Status);
            throw new DuplicatePaymentException(command.IdempotencyKey);
        }

        // PASSO 2: Mapear DTOs primitivos para value objects e enums de domínio.
        var planType = command.PlanType == "pro" ? PlanType.Pro : PlanType.Enterprise;
        var money = new Money(command.Amount, command.Currency);
        var method = command.PaymentMethod switch
        {
            "credit_card" => PaymentMethod.CreditCard,
            "pix" => PaymentMethod.Pix,
            "boleto" => PaymentMethod.Boleto,
            _ => throw new ArgumentException("Invalid payment method")
        };

        var payment = new PaymentEntity(
            command.UserId, planType, money, method, command.IdempotencyKey);

        // PASSO 3: Persistir a entidade de pagamento e o log de criação.
        _context.Payments.Add(payment);
        _context.PaymentLogs.Add(new PaymentLog(payment.Id, PaymentLog.EventTypes.Created));

        // PASSO 4: Transicionar para o status Processing e registrar log.
        payment.MarkProcessing();
        _context.PaymentLogs.Add(new PaymentLog(payment.Id, PaymentLog.EventTypes.Processing));

        // PASSO 5: Delegar ao gateway de pagamento conforme o método escolhido.
        Common.Models.PaymentResult result;
        try
        {
            result = method switch
            {
                PaymentMethod.CreditCard => await _gateway.ProcessCreditCardAsync(
                    money.Amount, command.CardNumber!, command.CardCvv!,
                    command.CardExpiryMonth!.Value, command.CardExpiryYear!.Value,
                    command.CardHolderName!),
                PaymentMethod.Pix => await _gateway.ProcessPixAsync(money.Amount),
                PaymentMethod.Boleto => await _gateway.ProcessBoletoAsync(money.Amount),
                _ => throw new ArgumentException("Invalid method")
            };
        }
        catch (Exception ex)
        {
            payment.MarkFailed();
            _context.PaymentLogs.Add(new PaymentLog(
                payment.Id, PaymentLog.EventTypes.Failed, new { error = ex.Message }));
            _logger.LogError(ex, "Payment failed: {PaymentId}", payment.Id);
            return new ProcessPaymentResponse(payment.Id, "failed", ex.Message);
        }

        // PASSO 6: Atualizar status conforme resultado do gateway e disparar evento de domínio.
        if (result.Success)
        {
            payment.MarkCompleted(result.GatewayPaymentId, result.RawResponse ?? "{}");
            _context.PaymentLogs.Add(new PaymentLog(
                payment.Id, PaymentLog.EventTypes.Completed));

            payment.AddDomainEvent(new PaymentCompletedDomainEvent
            {
                PaymentId = payment.Id,
                UserId = payment.UserId,
                PlanType = command.PlanType,
                Amount = money.Amount,
                Currency = money.Currency,
                PaymentMethod = command.PaymentMethod,
                PaidAt = payment.PaidAt!.Value
            });
        }
        else
        {
            payment.MarkFailed(result.GatewayMessage);
            _context.PaymentLogs.Add(new PaymentLog(
                payment.Id, PaymentLog.EventTypes.Failed,
                new { error = result.GatewayMessage }));

            payment.AddDomainEvent(new PaymentFailedDomainEvent
            {
                PaymentId = payment.Id,
                UserId = payment.UserId,
                ErrorMessage = result.GatewayMessage
            });
        }

        return MapToResponse(payment);
    }

    // Maps the Payment entity to a ProcessPaymentResponse DTO.
    private static ProcessPaymentResponse MapToResponse(PaymentEntity payment)
    {
        var errorMessage = payment.Status == PaymentStatus.Failed
            ? payment.GatewayResponse
            : null;

        return new ProcessPaymentResponse(
            payment.Id, payment.Status.ToString().ToLowerInvariant(), errorMessage);
    }
}
