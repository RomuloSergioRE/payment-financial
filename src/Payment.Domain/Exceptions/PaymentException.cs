namespace Payment.Domain.Exceptions;

// Base exception for all payment domain errors, including invalid state transitions
// and business rule violations.
public sealed class PaymentException : Exception
{
    // Create an exception with a descriptive message.
    public PaymentException(string message) : base(message) { }

    // Create an exception wrapping an inner exception from an underlying failure.
    public PaymentException(string message, Exception innerException)
        : base(message, innerException) { }
}
