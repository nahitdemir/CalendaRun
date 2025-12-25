using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Planning.Application.Common;
using Planning.Infrastructure;

namespace Planning.Application.Plans.Queries;

public class GetAdminPlansHandler : IQueryHandler<GetAdminPlansQuery, Result<List<AdminPlanDto>>>
{
    private readonly PlanningDbContext _db;
    private readonly ILogger<GetAdminPlansHandler> _logger;

    public GetAdminPlansHandler(PlanningDbContext db, ILogger<GetAdminPlansHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<List<AdminPlanDto>>> HandleAsync(GetAdminPlansQuery query, CancellationToken ct = default)
    {
        var plansQuery = _db.UserPlanItems.AsQueryable();

        // Super admin sees ALL plans, tenant admin sees only their tenant's plans
        if (!query.IsSuperAdmin)
        {
            if (!query.TenantId.HasValue)
                return Result<List<AdminPlanDto>>.Failure("Tenant ID is required");

            plansQuery = plansQuery.Where(p => p.TenantId == query.TenantId.Value);
        }

        var plans = await plansQuery
            .Include(p => p.User)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new AdminPlanDto(
                p.Id,
                p.TenantId,
                p.UserId,
                p.User.Email,
                p.EventId,
                p.State,
                p.CreatedAt,
                p.CreatedBy
            ))
            .ToListAsync(ct);

        _logger.LogInformation("Admin listed {Count} plans (SuperAdmin={IsSuperAdmin})", 
            plans.Count, query.IsSuperAdmin);

        return Result<List<AdminPlanDto>>.Success(plans);
    }
}
