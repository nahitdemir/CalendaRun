namespace Calendarun.Common.Errors;

/// <summary>
/// Base exception for all domain-level errors
/// </summary>
public abstract class DomainException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }

    protected DomainException(string code, string message, int statusCode = 400) 
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }
}

public class NotFoundException : DomainException
{
    public NotFoundException(string resource, object id) 
        : base("NOT_FOUND", $"{resource} with id '{id}' was not found", 404)
    {
    }
}

public class ConflictException : DomainException
{
    public ConflictException(string message) 
        : base("CONFLICT", message, 409)
    {
    }
}

public class ValidationException : DomainException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(string message, IDictionary<string, string[]>? errors = null) 
        : base("VALIDATION_ERROR", message, 400)
    {
        Errors = errors ?? new Dictionary<string, string[]>();
    }
}

public class ForbiddenException : DomainException
{
    public ForbiddenException(string message = "Access denied") 
        : base("FORBIDDEN", message, 403)
    {
    }
}

public class UnauthorizedException : DomainException
{
    public UnauthorizedException(string message = "Authentication required") 
        : base("UNAUTHORIZED", message, 401)
    {
    }
}

public class BusinessRuleException : DomainException
{
    public BusinessRuleException(string code, string message) 
        : base(code, message, 422)
    {
    }
}

