using Catalog.Application.AuditLogs.Queries;
using Catalog.Application.Common;
using Catalog.Application.Events.Commands;
using Catalog.Application.Events.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Event Query Handlers
        services.AddScoped<IQueryHandler<GetEventsQuery, Result<EventsPagedResult>>, GetEventsHandler>();
        services.AddScoped<IQueryHandler<GetEventByIdQuery, Result<EventDto>>, GetEventByIdHandler>();
        services.AddScoped<IQueryHandler<GetAdminEventsQuery, Result<List<AdminEventDto>>>, GetAdminEventsHandler>();

        // Event Command Handlers
        services.AddScoped<ICommandHandler<CreateEventCommand, Result<CreateEventResult>>, CreateEventHandler>();
        services.AddScoped<ICommandHandler<UpdateEventCommand, Result<UpdateEventResult>>, UpdateEventHandler>();
        services.AddScoped<ICommandHandler<DeleteEventCommand, Result>, DeleteEventHandler>();

        // Audit Log Query Handlers
        services.AddScoped<IQueryHandler<GetAuditLogsQuery, Result<AuditLogPagedResult>>, GetAuditLogsHandler>();

        return services;
    }
}

