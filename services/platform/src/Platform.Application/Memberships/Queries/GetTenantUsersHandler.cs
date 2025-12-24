using Microsoft.EntityFrameworkCore;
using Platform.Application.Common;
using Platform.Domain.Entities;
using Platform.Infrastructure.Data;

namespace Platform.Application.Memberships.Queries;

public class GetTenantUsersHandler : IQueryHandler<GetTenantUsersQuery, Result<List<TenantUserDto>>>
{
    private readonly PlatformDbContext _db;

    public GetTenantUsersHandler(PlatformDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<TenantUserDto>>> HandleAsync(GetTenantUsersQuery query, CancellationToken ct = default)
    {
        var users = await _db.Memberships
            .Where(m => m.TenantId == query.TenantId && m.Status == MembershipStatus.Active)
            .Select(m => new TenantUserDto(
                m.Id,
                m.UserId,
                m.UserEmail,
                m.Role.ToString(),
                m.CreatedAt,
                m.AcceptedAt
            ))
            .ToListAsync(ct);

        return Result<List<TenantUserDto>>.Success(users);
    }
}

