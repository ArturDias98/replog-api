namespace replog_application;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    private Result(T value) { IsSuccess = true; Value = value; }
    private Result(string errorCode, string errorMessage) { IsSuccess = false; ErrorCode = errorCode; ErrorMessage = errorMessage; }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string errorCode, string errorMessage) => new(errorCode, errorMessage);
}
