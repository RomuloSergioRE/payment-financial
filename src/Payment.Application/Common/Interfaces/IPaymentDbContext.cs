using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PaymentEntity = Payment.Domain.Entities.Payment;

namespace Payment.Application.Common.Interfaces;

// Abstraction over the application's database context,
// exposing entity sets and persistence operations.
public interface IPaymentDbContext
{
    DbSet<PaymentEntity> Payments { get; }
    DbSet<Payment.Domain.Entities.PaymentLog> PaymentLogs { get; }
    DbSet<Payment.Domain.Entities.OutboxMessage> OutboxMessages { get; }
    DatabaseFacade Database { get; }
    ChangeTracker ChangeTracker { get; }
    // Persists all pending changes to the database.
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
