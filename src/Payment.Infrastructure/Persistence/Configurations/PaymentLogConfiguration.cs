using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence.Configurations;

public sealed class PaymentLogConfiguration : IEntityTypeConfiguration<PaymentLog>
{
    public void Configure(EntityTypeBuilder<PaymentLog> builder)
    {
        builder.ToTable("payment_logs");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(l => l.PaymentId)
            .HasColumnName("payment_id")
            .IsRequired();

        builder.HasOne<Domain.Entities.Payment>()
            .WithMany()
            .HasForeignKey(l => l.PaymentId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_payment_logs_payment");

        builder.Property(l => l.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb");

        builder.Property(l => l.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.HasIndex(l => l.PaymentId)
            .HasDatabaseName("idx_payment_logs_payment_id");

        builder.HasIndex(l => l.EventType)
            .HasDatabaseName("idx_payment_logs_event_type");
    }
}
