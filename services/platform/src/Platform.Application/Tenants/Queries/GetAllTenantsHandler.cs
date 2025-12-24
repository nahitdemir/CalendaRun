using Microsoft.EntityFrameworkCore;
using Platform.Application.Common;
using Platform.Domain.Entities;
using Platform.Infrastructure.Data;

namespace Platform.Application.Tenants.Queries;

public class GetAllTenantsHandler : IQueryHandler<GetAllTenantsQuery, Result<List<TenantDto>>>
{
    private readonly PlatformDbContext _db;

    public GetAllTenantsHandler(PlatformDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<TenantDto>>> HandleAsync(GetAllTenantsQuery query, CancellationToken ct = default)
    {
        var tenants = await _db.Tenants
            .Where(t => t.Status != TenantStatus.Deleted)
            .Select(t => new TenantDto(
                t.Id,
                t.Name,
                t.Slug,
                t.DefaultLanguage,
                t.DefaultCurrency,
                t.Status.ToString(),
                t.CreatedAt,
                t.Memberships.Count(m => m.Status == MembershipStatus.Active)
            ))
            .ToListAsync(ct);

        return Result<List<TenantDto>>.Success(tenants);
    }
}

