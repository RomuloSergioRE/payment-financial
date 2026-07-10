using Microsoft.EntityFrameworkCore;
using Payment.Application.Common.Interfaces;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence;

public sealed class PaymentDbContext : DbContext, IPaymentDbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
        : base(options) { }

    public DbSet<Domain.Entities.Payment> Payments => Set<Domain.Entities.Payment>();
    public DbSet<PaymentLog> PaymentLogs => Set<PaymentLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
