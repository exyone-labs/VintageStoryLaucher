namespace VSL.Application;

public sealed class OperationResult<T>
{
    public static OperationResult<T> Success(T value, string? message = null) => new(value, true, message, null);

    public static OperationResult<T> Failed(string message, Exception? exception = null) => new(default, false, message, exception);

    private OperationResult(T? value, bool isSuccess, string? message, Exception? exception)
    {
        Value = value;
        IsSuccess = isSuccess;
        Message = message;
        Exception = exception;
    }

    public T? Value { get; }

    public bool IsSuccess { get; }

    public string? Message { get; }

    public Exception? Exception { get; }
}
