using Microsoft.EntityFrameworkCore;
using Planning.Application.Common;
using Planning.Domain;
using Planning.Infrastructure;

namespace Planning.Application.Plans.Queries;

public class GetUserPlansHandler : IQueryHandler<GetUserPlansQuery, Result<List<PlanDto>>>
{
    private readonly PlanningDbContext _db;

    public GetUserPlansHandler(PlanningDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<PlanDto>>> HandleAsync(GetUserPlansQuery query, CancellationToken ct = default)
    {
        var plansQuery = _db.UserPlanItems
            .Where(p => p.UserId == query.UserId && p.State == PlanState.Active);

        // Filter by tenant if provided
        if (query.TenantId.HasValue)
        {
            plansQuery = plansQuery.Where(p => p.TenantId == query.TenantId.Value);
        }

        var plans = await plansQuery
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PlanDto(
                p.Id,
                p.EventId,
                p.State,
                p.CreatedAt
            ))
            .ToListAsync(ct);

        return Result<List<PlanDto>>.Success(plans);
    }
}

