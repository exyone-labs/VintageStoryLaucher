namespace VSL.Application;

public sealed class OperationResult
{
    public static OperationResult Success(string? message = null) => new(true, message, null);

    public static OperationResult Failed(string message, Exception? exception = null) => new(false, message, exception);

    private OperationResult(bool isSuccess, string? message, Exception? exception)
    {
        IsSuccess = isSuccess;
        Message = message;
        Exception = exception;
    }

    public bool IsSuccess { get; }

    public string? Message { get; }

    public Exception? Exception { get; }
}
