namespace Calendarun.Common.UseCases;

/// <summary>
/// Result type for operations that can fail
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }
    public string? ErrorCode { get; }

    protected Result(bool isSuccess, string? error, string? errorCode)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
    }

    public static Result Success() => new(true, null, null);
    public static Result Failure(string error, string? errorCode = null) => new(false, error, errorCode);

    public static Result<T> Success<T>(T value) => new(value, true, null, null);
    public static Result<T> Failure<T>(string error, string? errorCode = null) => new(default!, false, error, errorCode);
}

/// <summary>
/// Result type with value
/// </summary>
public class Result<T> : Result
{
    public T Value { get; }

    internal Result(T value, bool isSuccess, string? error, string? errorCode) 
        : base(isSuccess, error, errorCode)
    {
        Value = value;
    }

    public static implicit operator Result<T>(T value) => Success(value);
}

