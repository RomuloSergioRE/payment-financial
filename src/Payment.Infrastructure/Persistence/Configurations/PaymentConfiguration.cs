using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Entities;
using Payment.Domain.Enums;

namespace Payment.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Domain.Entities.Payment>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(p => p.PlanType)
            .HasColumnName("plan_type")
            .HasConversion(
                v => v.ToString().ToLowerInvariant(),
                v => Enum.Parse<PlanType>(v, true))
            .HasMaxLength(20)
            .IsRequired();

        builder.OwnsOne(p => p.Amount, amount =>
        {
            amount.Property(a => a.Amount)
                .HasColumnName("amount")
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            amount.Property(a => a.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .HasDefaultValue("BRL")
                .IsRequired();
        });

        builder.Property(p => p.Method)
            .HasColumnName("payment_method")
            .HasConversion(
                v => v.ToString().ToLowerInvariant(),
                v => Enum.Parse<PaymentMethod>(v, true))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasConversion(
                v => v.ToString().ToLowerInvariant(),
                v => Enum.Parse<PaymentStatus>(v, true))
            .HasMaxLength(20)
            .HasDefaultValue(PaymentStatus.Pending)
            .IsRequired();

        builder.Property(p => p.GatewayPaymentId)
            .HasColumnName("gateway_payment_id")
            .HasMaxLength(100);

        builder.Property(p => p.GatewayResponse)
            .HasColumnName("gateway_response")
            .HasColumnType("jsonb");

        builder.Property(p => p.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(100);

        builder.HasIndex(p => p.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("uq_payments_idempotency");

        builder.Property(p => p.PaidAt)
            .HasColumnName("paid_at")
            .HasColumnType("timestamptz");

        builder.Property(p => p.RefundedAt)
            .HasColumnName("refunded_at")
            .HasColumnType("timestamptz");

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.HasIndex(p => p.UserId)
            .HasDatabaseName("idx_payments_user_id");

        builder.HasIndex(p => p.Status)
            .HasDatabaseName("idx_payments_status");

        builder.HasIndex(p => p.CreatedAt)
            .HasDatabaseName("idx_payments_created_at");
    }
}
