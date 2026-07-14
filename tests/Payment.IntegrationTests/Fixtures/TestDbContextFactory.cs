using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;
using PaymentDbContext = global::Payment.Infrastructure.Persistence.PaymentDbContext;
using PaymentEntity = global::Payment.Domain.Entities.Payment;

namespace Payment.IntegrationTests.Fixtures;

// Factory for creating in-memory PaymentDbContext and seeding test data for integration tests.
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

    // Seeds the database with a pending payment and its initial PaymentLog entry for use in integration tests.
    public static void SeedData(PaymentDbContext context)
    {
        // Arrange
        var payment = new PaymentEntity(
            Guid.NewGuid(),
            PlanType.Pro,
            new Money(29.90m, "BRL"),
            PaymentMethod.CreditCard,
            "existing-idempotency-key");

        // Act
        context.Payments.Add(payment);
        context.PaymentLogs.Add(new PaymentLog(payment.Id, "payment.created"));
        context.SaveChanges();
    }
}
