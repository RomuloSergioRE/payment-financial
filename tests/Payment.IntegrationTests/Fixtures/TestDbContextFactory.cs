using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;
using PaymentDbContext = global::Payment.Infrastructure.Persistence.PaymentDbContext;
using PaymentEntity = global::Payment.Domain.Entities.Payment;

namespace Payment.IntegrationTests.Fixtures;

public static class TestDbContextFactory
{
    public static PaymentDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new PaymentDbContext(options);
    }

    public static void SeedData(PaymentDbContext context)
    {
        var payment = new PaymentEntity(
            Guid.NewGuid(),
            PlanType.Pro,
            new Money(29.90m, "BRL"),
            PaymentMethod.CreditCard,
            "existing-idempotency-key");

        context.Payments.Add(payment);
        context.PaymentLogs.Add(new PaymentLog(payment.Id, "payment.created"));
        context.SaveChanges();
    }
}
