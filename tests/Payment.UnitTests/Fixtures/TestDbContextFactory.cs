using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;
using PaymentDbContext = global::Payment.Infrastructure.Persistence.PaymentDbContext;
using PaymentEntity = global::Payment.Domain.Entities.Payment;

namespace Payment.UnitTests.Fixtures;

public static class TestDbContextFactory
{
    public static PaymentDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new PaymentDbContext(options);
    }

    public static async Task<PaymentEntity> CreatePendingPaymentAsync(
        PaymentDbContext context,
        Guid? userId = null,
        string? idempotencyKey = null)
    {
        var payment = new PaymentEntity(
            userId ?? Guid.NewGuid(),
            PlanType.Pro,
            new Money(29.90m, "BRL"),
            PaymentMethod.CreditCard,
            idempotencyKey ?? Guid.NewGuid().ToString());

        context.Payments.Add(payment);
        context.PaymentLogs.Add(new PaymentLog(payment.Id, PaymentLog.EventTypes.Created));
        await context.SaveChangesAsync();
        return payment;
    }

    public static async Task<PaymentEntity> CreateCompletedPaymentAsync(
        PaymentDbContext context,
        Guid? userId = null,
        string? idempotencyKey = null)
    {
        var payment = new PaymentEntity(
            userId ?? Guid.NewGuid(),
            PlanType.Pro,
            new Money(29.90m, "BRL"),
            PaymentMethod.CreditCard,
            idempotencyKey ?? Guid.NewGuid().ToString());

        payment.MarkProcessing();
        payment.MarkCompleted("gw_test", "{\"status\":\"approved\"}");

        context.Payments.Add(payment);
        context.PaymentLogs.Add(new PaymentLog(payment.Id, PaymentLog.EventTypes.Created));
        context.PaymentLogs.Add(new PaymentLog(payment.Id, PaymentLog.EventTypes.Processing));
        context.PaymentLogs.Add(new PaymentLog(payment.Id, PaymentLog.EventTypes.Completed));
        await context.SaveChangesAsync();
        return payment;
    }
}
