using Microsoft.EntityFrameworkCore;
using Planning.Application.Common;
using Planning.Domain;
using Planning.Infrastructure;

namespace Planning.Application.Users.Queries;

public class GetAdminUsersHandler : IQueryHandler<GetAdminUsersQuery, Result<List<AdminUserDto>>>
{
    private readonly PlanningDbContext _db;

    public GetAdminUsersHandler(PlanningDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<AdminUserDto>>> HandleAsync(GetAdminUsersQuery query, CancellationToken ct = default)
    {
        var usersQuery = _db.Users.AsQueryable();

        if (!query.IsSuperAdmin)
        {
            if (!query.TenantId.HasValue)
                return Result<List<AdminUserDto>>.Failure("Tenant ID is required");

            usersQuery = usersQuery.Where(u => u.TenantId == query.TenantId.Value);
        }

        var users = await usersQuery
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new AdminUserDto(
                u.Id,
                u.TenantId,
                u.Email,
                u.CreatedAt,
                _db.UserPlanItems.Count(p => p.UserId == u.Id && p.State == PlanState.Active)
            ))
            .ToListAsync(ct);

        return Result<List<AdminUserDto>>.Success(users);
    }
}
