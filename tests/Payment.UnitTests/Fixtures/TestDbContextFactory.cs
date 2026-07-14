using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;
using PaymentDbContext = global::Payment.Infrastructure.Persistence.PaymentDbContext;
using PaymentEntity = global::Payment.Domain.Entities.Payment;

namespace Payment.UnitTests.Fixtures;

// Factory for creating in-memory PaymentDbContext instances and seeding test payment data.
public static class TestDbContextFactory
{
    // Creates a new in-memory PaymentDbContext with a unique database name for test isolation.
    public static PaymentDbContext CreateInMemoryContext()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act & Assert
        return new PaymentDbContext(options);
    }

    // Creates and persists a pending payment with its initial PaymentLog entry.
    public static async Task<PaymentEntity> CreatePendingPaymentAsync(
        PaymentDbContext context,
        Guid? userId = null,
        string? idempotencyKey = null)
    {
        // Arrange
        var payment = new PaymentEntity(
            userId ?? Guid.NewGuid(),
            PlanType.Pro,
            new Money(29.90m, "BRL"),
            PaymentMethod.CreditCard,
            idempotencyKey ?? Guid.NewGuid().ToString());

        // Act
        context.Payments.Add(payment);
        context.PaymentLogs.Add(new PaymentLog(payment.Id, PaymentLog.EventTypes.Created));
        await context.SaveChangesAsync();
        return payment;
    }

    // Creates and persists a completed payment with Created, Processing, and Completed log entries.
    public static async Task<PaymentEntity> CreateCompletedPaymentAsync(
        PaymentDbContext context,
        Guid? userId = null,
        string? idempotencyKey = null)
    {
        // Arrange
        var payment = new PaymentEntity(
            userId ?? Guid.NewGuid(),
            PlanType.Pro,
            new Money(29.90m, "BRL"),
            PaymentMethod.CreditCard,
            idempotencyKey ?? Guid.NewGuid().ToString());

        // Act
        payment.MarkProcessing();
        payment.MarkCompleted("gw_test", "{\"status\":\"approved\"}");

        // Assert
        context.Payments.Add(payment);
        context.PaymentLogs.Add(new PaymentLog(payment.Id, PaymentLog.EventTypes.Created));
        context.PaymentLogs.Add(new PaymentLog(payment.Id, PaymentLog.EventTypes.Processing));
        context.PaymentLogs.Add(new PaymentLog(payment.Id, PaymentLog.EventTypes.Completed));
        await context.SaveChangesAsync();
        return payment;
    }
}
