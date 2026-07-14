namespace Payment.Domain.Enums;

// Possible states of a Payment through its lifecycle.
public enum PaymentStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Refunded,
    Cancelled
}
