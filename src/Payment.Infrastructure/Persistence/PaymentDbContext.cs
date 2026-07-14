using Microsoft.EntityFrameworkCore;
using Payment.Application.Common.Interfaces;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence;

// EF Core DbContext responsible for mapping domain entities to PostgreSQL tables.
// Applies all IEntityTypeConfiguration implementations from this assembly.
public sealed class PaymentDbContext : DbContext, IPaymentDbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
        : base(options) { }

    public DbSet<Domain.Entities.Payment> Payments => Set<Domain.Entities.Payment>();
    public DbSet<PaymentLog> PaymentLogs => Set<PaymentLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Auto-discovers and applies all IEntityTypeConfiguration classes in the Infrastructure assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
