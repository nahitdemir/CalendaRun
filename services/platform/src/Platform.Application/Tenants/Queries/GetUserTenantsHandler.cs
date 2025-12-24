using Microsoft.EntityFrameworkCore;
using Platform.Application.Common;
using Platform.Domain.Entities;
using Platform.Infrastructure.Data;

namespace Platform.Application.Tenants.Queries;

public class GetUserTenantsHandler : IQueryHandler<GetUserTenantsQuery, Result<List<UserTenantDto>>>
{
    private readonly PlatformDbContext _db;

    public GetUserTenantsHandler(PlatformDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<UserTenantDto>>> HandleAsync(GetUserTenantsQuery query, CancellationToken ct = default)
    {
        var memberships = await _db.Memberships
            .Where(m => m.UserId == query.UserId && m.Status == MembershipStatus.Active)
            .Include(m => m.Tenant)
            .Where(m => m.Tenant.Status == TenantStatus.Active)
            .Select(m => new UserTenantDto(
                m.TenantId,
                m.Tenant.Name,
                m.Tenant.Slug,
                m.Role.ToString(),
                m.CreatedAt
            ))
            .ToListAsync(ct);

        return Result<List<UserTenantDto>>.Success(memberships);
    }
}

