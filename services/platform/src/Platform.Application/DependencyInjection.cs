using Microsoft.Extensions.DependencyInjection;
using Platform.Application.AuditLogs.Queries;
using Platform.Application.Common;
using Platform.Application.Invites.Commands;
using Platform.Application.Memberships.Queries;
using Platform.Application.Tenants.Commands;
using Platform.Application.Tenants.Queries;

namespace Platform.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Tenant Command Handlers
        services.AddScoped<ICommandHandler<CreateTenantCommand, Result<CreateTenantResult>>, CreateTenantHandler>();

        // Tenant Query Handlers
        services.AddScoped<IQueryHandler<GetAllTenantsQuery, Result<List<TenantDto>>>, GetAllTenantsHandler>();
        services.AddScoped<IQueryHandler<GetUserTenantsQuery, Result<List<UserTenantDto>>>, GetUserTenantsHandler>();

        // Invite Command Handlers
        services.AddScoped<ICommandHandler<CreateInviteCommand, Result<CreateInviteResult>>, CreateInviteHandler>();
        services.AddScoped<ICommandHandler<AcceptInviteCommand, Result<AcceptInviteResult>>, AcceptInviteHandler>();

        // Membership Query Handlers
        services.AddScoped<IQueryHandler<GetTenantUsersQuery, Result<List<TenantUserDto>>>, GetTenantUsersHandler>();
        services.AddScoped<IQueryHandler<ValidateMembershipQuery, MembershipValidationResult>, ValidateMembershipHandler>();

        // Audit Log Query Handlers
        services.AddScoped<IQueryHandler<GetAuditLogsQuery, Result<AuditLogPagedResult>>, GetAuditLogsHandler>();

        return services;
    }
}

