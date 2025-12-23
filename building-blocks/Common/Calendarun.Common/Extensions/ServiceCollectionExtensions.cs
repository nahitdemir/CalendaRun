using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Calendarun.Common.Errors;

namespace Calendarun.Common.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds CalendaRun common services
    /// </summary>
    public static IServiceCollection AddCalendarunCommon(this IServiceCollection services)
    {
        services.AddProblemDetails();
        return services;
    }

    /// <summary>
    /// Adds CalendaRun exception handler middleware
    /// </summary>
    public static IApplicationBuilder UseCalendarunExceptionHandler(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlerMiddleware>();
        return app;
    }
}

