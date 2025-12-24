namespace Catalog.Application.Common;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public ResultErrorType ErrorType { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
        Error = null;
        ErrorType = ResultErrorType.None;
    }

    private Result(string error, ResultErrorType errorType)
    {
        IsSuccess = false;
        Value = default;
        Error = error;
        ErrorType = errorType;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error, ResultErrorType errorType = ResultErrorType.Validation) 
        => new(error, errorType);
    public static Result<T> NotFound(string error) => new(error, ResultErrorType.NotFound);
    public static Result<T> Forbidden(string error = "Access denied") => new(error, ResultErrorType.Forbidden);
    public static Result<T> Conflict(string error) => new(error, ResultErrorType.Conflict);
}

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public ResultErrorType ErrorType { get; }

    private Result()
    {
        IsSuccess = true;
        Error = null;
        ErrorType = ResultErrorType.None;
    }

    private Result(string error, ResultErrorType errorType)
    {
        IsSuccess = false;
        Error = error;
        ErrorType = errorType;
    }

    public static Result Success() => new();
    public static Result Failure(string error, ResultErrorType errorType = ResultErrorType.Validation) 
        => new(error, errorType);
    public static Result NotFound(string error) => new(error, ResultErrorType.NotFound);
    public static Result Forbidden(string error = "Access denied") => new(error, ResultErrorType.Forbidden);
}

public enum ResultErrorType
{
    None,
    Validation,
    NotFound,
    Forbidden,
    Conflict,
    Internal
}

