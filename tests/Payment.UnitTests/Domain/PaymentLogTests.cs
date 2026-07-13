using System.Text.Json;
using FluentAssertions;
using Payment.Domain.Entities;

namespace Payment.UnitTests.Domain;

public class PaymentLogTests
{
    [Fact]
    public void Constructor_WithMetadata_SerializesToJson()
    {
        var paymentId = Guid.NewGuid();
        var metadata = new { error = "Card declined" };

        var log = new PaymentLog(paymentId, "payment.failed", metadata);

        log.PaymentId.Should().Be(paymentId);
        log.EventType.Should().Be("payment.failed");
        log.Metadata.Should().NotBeNull();

        var doc = JsonDocument.Parse(log.Metadata!);
        doc.RootElement.GetProperty("error").GetString().Should().Be("Card declined");
    }

    [Fact]
    public void Constructor_WithoutMetadata_MetadataIsNull()
    {
        var log = new PaymentLog(Guid.NewGuid(), "payment.created");

        log.Metadata.Should().BeNull();
    }

    [Fact]
    public void Constructor_SetsIdAndCreatedAt()
    {
        var log = new PaymentLog(Guid.NewGuid(), "payment.created");

        log.Id.Should().NotBeEmpty();
        log.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
