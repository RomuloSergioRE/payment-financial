namespace Payment.Application.Common.Exceptions;

// Thrown when a requested entity cannot be found in the database.
public sealed class NotFoundException : Exception
{
    // Creates an exception with entity name and key for a descriptive message.
    public NotFoundException(string name, object key)
        : base($"{name} with key {key} was not found")
    {
    }

    // Creates an exception with a custom message.
    public NotFoundException(string message)
        : base(message)
    {
    }
}
