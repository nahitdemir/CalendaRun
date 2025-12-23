using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Calendarun.Common.Errors;

public static class ProblemDetailsFactory
{
    public static ProblemDetails Create(DomainException exception, HttpContext? context = null)
    {
        var problemDetails = new ProblemDetails
        {
            Type = $"https://calendarun.local/errors/{exception.Code.ToLower()}",
            Title = GetTitle(exception.StatusCode),
            Status = exception.StatusCode,
            Detail = exception.Message,
            Instance = context?.Request.Path
        };

        problemDetails.Extensions["code"] = exception.Code;
        problemDetails.Extensions["traceId"] = context?.TraceIdentifier;

        if (exception is ValidationException validationException && validationException.Errors.Any())
        {
            problemDetails.Extensions["errors"] = validationException.Errors;
        }

        return problemDetails;
    }

    public static ProblemDetails Create(int statusCode, string detail, HttpContext? context = null)
    {
        return new ProblemDetails
        {
            Type = $"https://calendarun.local/errors/{GetCode(statusCode)}",
            Title = GetTitle(statusCode),
            Status = statusCode,
            Detail = detail,
            Instance = context?.Request.Path,
            Extensions =
            {
                ["traceId"] = context?.TraceIdentifier
            }
        };
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        422 => "Unprocessable Entity",
        500 => "Internal Server Error",
        _ => "Error"
    };

    private static string GetCode(int statusCode) => statusCode switch
    {
        400 => "bad-request",
        401 => "unauthorized",
        403 => "forbidden",
        404 => "not-found",
        409 => "conflict",
        422 => "unprocessable-entity",
        500 => "internal-error",
        _ => "error"
    };
}

