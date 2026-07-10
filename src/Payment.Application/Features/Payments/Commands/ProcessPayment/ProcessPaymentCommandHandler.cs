using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Payment.Application.Common.Interfaces;
using Payment.Application.Common.Models;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;
using PaymentEntity = Payment.Domain.Entities.Payment;

namespace Payment.Application.Features.Payments.Commands.ProcessPayment;

public sealed class ProcessPaymentCommandHandler
    : IRequestHandler<ProcessPaymentCommand, ProcessPaymentResponse>
{
    private readonly IPaymentDbContext _context;
    private readonly IPaymentGateway _gateway;
    private readonly IMessageBus _bus;
    private readonly ILogger<ProcessPaymentCommandHandler> _logger;

    public ProcessPaymentCommandHandler(
        IPaymentDbContext context,
        IPaymentGateway gateway,
        IMessageBus bus,
        ILogger<ProcessPaymentCommandHandler> logger)
    {
        _context = context;
        _gateway = gateway;
        _bus = bus;
        _logger = logger;
    }

    public async Task<ProcessPaymentResponse> Handle(
        ProcessPaymentCommand command,
        CancellationToken cancellationToken)
    {
        var existing = await _context.Payments
            .FirstOrDefaultAsync(p => p.IdempotencyKey == command.IdempotencyKey,
                                 cancellationToken);

        if (existing is not null)
        {
            _logger.LogWarning(
                "Duplicate payment attempt. IdempotencyKey: {Key}, ExistingStatus: {Status}",
                command.IdempotencyKey, existing.Status);
            return MapToResponse(existing);
        }

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

        _context.Payments.Add(payment);
        _context.PaymentLogs.Add(new PaymentLog(payment.Id, "payment.created"));

        payment.MarkProcessing();
        _context.PaymentLogs.Add(new PaymentLog(payment.Id, "payment.processing"));

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
                payment.Id, "payment.failed", new { error = ex.Message }));
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogError(ex, "Payment failed: {PaymentId}", payment.Id);
            return new ProcessPaymentResponse(payment.Id, "failed", ex.Message);
        }

        if (result.Success)
        {
            payment.MarkCompleted(result.GatewayPaymentId, result.RawResponse ?? "{}");
            _context.PaymentLogs.Add(new PaymentLog(
                payment.Id, "payment.completed"));

            var paymentEvent = new PaymentCompletedEvent(
                EventId: Guid.NewGuid().ToString(),
                Type: "payment.completed",
                Timestamp: DateTime.UtcNow,
                Data: new PaymentCompletedData(
                    payment.Id, payment.UserId, command.PlanType,
                    money.Amount, money.Currency, command.PaymentMethod,
                    payment.PaidAt!.Value));

            await _bus.PublishAsync(paymentEvent, "payment.completed", cancellationToken);
        }
        else
        {
            payment.MarkFailed();
            _context.PaymentLogs.Add(new PaymentLog(
                payment.Id, "payment.failed",
                new { error = result.GatewayMessage }));
        }

        await _context.SaveChangesAsync(cancellationToken);

        return MapToResponse(payment);
    }

    private static ProcessPaymentResponse MapToResponse(PaymentEntity payment)
    {
        return new ProcessPaymentResponse(
            payment.Id, payment.Status.ToString().ToLowerInvariant(), null);
    }
}
