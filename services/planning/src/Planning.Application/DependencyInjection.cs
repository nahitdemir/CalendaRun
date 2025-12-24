using Microsoft.Extensions.DependencyInjection;
using Planning.Application.AuditLogs.Queries;
using Planning.Application.Common;
using Planning.Application.Plans.Commands;
using Planning.Application.Plans.Queries;
using Planning.Application.Users.Queries;

namespace Planning.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Plan Command Handlers
        services.AddScoped<ICommandHandler<CreatePlanCommand, Result<CreatePlanResult>>, CreatePlanHandler>();
        services.AddScoped<ICommandHandler<DeletePlanCommand, Result>, DeletePlanHandler>();

        // Plan Query Handlers
        services.AddScoped<IQueryHandler<GetUserPlansQuery, Result<List<PlanDto>>>, GetUserPlansHandler>();
        services.AddScoped<IQueryHandler<GetAdminPlansQuery, Result<List<AdminPlanDto>>>, GetAdminPlansHandler>();

        // User Query Handlers
        services.AddScoped<IQueryHandler<GetAdminUsersQuery, Result<List<AdminUserDto>>>, GetAdminUsersHandler>();

        // Audit Log Query Handlers
        services.AddScoped<IQueryHandler<GetAuditLogsQuery, Result<AuditLogPagedResult>>, GetAuditLogsHandler>();

        return services;
    }
}

