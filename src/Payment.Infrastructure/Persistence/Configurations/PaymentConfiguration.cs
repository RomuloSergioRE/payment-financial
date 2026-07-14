using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Entities;
using Payment.Domain.Enums;

namespace Payment.Infrastructure.Persistence.Configurations;

// Entity Framework configuration for the Payment aggregate root.
// Maps the Payment entity to the "payments" table with enums stored as lowercase strings,
// owned Amount value object, and database-level check constraints.
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

        // Store PlanType enum as lowercase string (e.g., "pro", "enterprise")
        builder.Property(p => p.PlanType)
            .HasColumnName("plan_type")
            .HasConversion(
                v => v.ToString().ToLowerInvariant(),
                v => Enum.Parse<PlanType>(v, true))
            .HasMaxLength(20)
            .IsRequired();

        // Map the owned Money value object with Amount and Currency columns
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

        // Store PaymentMethod enum as lowercase string (e.g., "credit_card", "pix", "boleto")
        builder.Property(p => p.Method)
            .HasColumnName("payment_method")
            .HasConversion(
                v => v.ToString().ToLowerInvariant(),
                v => Enum.Parse<PaymentMethod>(v, true))
            .HasMaxLength(20)
            .IsRequired();

        // Store PaymentStatus enum as lowercase string with "pending" as default
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

        // Unique index prevents duplicate payment submissions with the same idempotency key
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

        // Performance indexes for common query patterns
        builder.HasIndex(p => p.UserId)
            .HasDatabaseName("idx_payments_user_id");

        builder.HasIndex(p => p.Status)
            .HasDatabaseName("idx_payments_status");

        builder.HasIndex(p => p.CreatedAt)
            .HasDatabaseName("idx_payments_created_at");

        // Database-level check constraints to enforce valid enum values and amount > 0
        builder.ToTable("payments", t =>
        {
            t.HasCheckConstraint("ck_payments_amount", "amount > 0");
            t.HasCheckConstraint("ck_payments_status",
                "status IN ('pending','processing','completed','failed','refunded','cancelled')");
            t.HasCheckConstraint("ck_payments_method",
                "payment_method IN ('credit_card','pix','boleto')");
            t.HasCheckConstraint("ck_payments_plan",
                "plan_type IN ('pro','enterprise')");
        });
    }
}
