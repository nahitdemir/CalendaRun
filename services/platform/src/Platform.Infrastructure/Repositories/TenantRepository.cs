using Microsoft.EntityFrameworkCore;
using Platform.Domain.Entities;
using Platform.Infrastructure.Data;

namespace Platform.Infrastructure.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly PlatformDbContext _db;

    public TenantRepository(PlatformDbContext db)
    {
        _db = db;
    }

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Tenants
            .Include(t => t.Memberships)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await _db.Tenants
            .Include(t => t.Memberships)
            .FirstOrDefaultAsync(t => t.Slug == slug, ct);
    }

    public async Task<IEnumerable<Tenant>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Tenants
            .Where(t => t.Status == TenantStatus.Active)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<Tenant> CreateAsync(Tenant tenant, CancellationToken ct = default)
    {
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);
        return tenant;
    }

    public async Task<Tenant> UpdateAsync(Tenant tenant, CancellationToken ct = default)
    {
        _db.Tenants.Update(tenant);
        await _db.SaveChangesAsync(ct);
        return tenant;
    }
}

