using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PaymentEntity = Payment.Domain.Entities.Payment;

namespace Payment.Application.Common.Interfaces;

public interface IPaymentDbContext
{
    DbSet<PaymentEntity> Payments { get; }
    DbSet<Payment.Domain.Entities.PaymentLog> PaymentLogs { get; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
