namespace Payment.Application.Common.Models;

// Generic result wrapper that represents either a success or a failure
// without throwing exceptions, following the Result pattern.
public sealed record Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }

    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    // Creates a success result with the given value.
    public static Result<T> Success(T value) => new(true, value, null);

    // Creates a failure result with the given error message.
    public static Result<T> Failure(string error) => new(false, default, error);
}
