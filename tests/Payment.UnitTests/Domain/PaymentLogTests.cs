using System.Text.Json;
using FluentAssertions;
using Payment.Domain.Entities;

namespace Payment.UnitTests.Domain;

// Tests for the PaymentLog entity: construction, metadata serialization, and auto-generated fields.
public class PaymentLogTests
{
    // Given a paymentId, event type, and metadata object, When creating a PaymentLog, Then metadata is serialized to JSON.
    [Fact]
    public void Constructor_WithMetadata_SerializesToJson()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var metadata = new { error = "Card declined" };

        // Act
        var log = new PaymentLog(paymentId, "payment.failed", metadata);

        // Assert
        log.PaymentId.Should().Be(paymentId);
        log.EventType.Should().Be("payment.failed");
        log.Metadata.Should().NotBeNull();

        var doc = JsonDocument.Parse(log.Metadata!);
        doc.RootElement.GetProperty("error").GetString().Should().Be("Card declined");
    }

    // Given no metadata, When creating a PaymentLog, Then Metadata is null.
    [Fact]
    public void Constructor_WithoutMetadata_MetadataIsNull()
    {
        // Act
        var log = new PaymentLog(Guid.NewGuid(), "payment.created");

        // Assert
        log.Metadata.Should().BeNull();
    }

    // Given a new PaymentLog, When created, Then Id is generated and CreatedAt is set to now.
    [Fact]
    public void Constructor_SetsIdAndCreatedAt()
    {
        // Act
        var log = new PaymentLog(Guid.NewGuid(), "payment.created");

        // Assert
        log.Id.Should().NotBeEmpty();
        log.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
