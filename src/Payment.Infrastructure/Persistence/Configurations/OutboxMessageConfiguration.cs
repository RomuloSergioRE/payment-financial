using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence.Configurations;

// Entity Framework configuration for the OutboxMessage entity.
// Implements the Transactional Outbox pattern: messages are persisted in the same
// database transaction as domain changes, then dispatched asynchronously by a background worker.
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.EventType)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Payload)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamptz");

        builder.Property(x => x.ProcessedAt)
            .HasColumnType("timestamptz");

        builder.Property(x => x.Error)
            .HasMaxLength(2000);

        // Filtered index for efficient polling of unprocessed messages only
        builder.HasIndex(x => x.ProcessedAt)
            .HasFilter("\"ProcessedAt\" IS NULL");
    }
}
