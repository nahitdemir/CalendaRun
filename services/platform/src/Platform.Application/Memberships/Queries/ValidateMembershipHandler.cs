using Microsoft.EntityFrameworkCore;
using Platform.Application.Common;
using Platform.Domain.Entities;
using Platform.Infrastructure.Data;
using StackExchange.Redis;
using System.Text.Json;

namespace Platform.Application.Memberships.Queries;

public class ValidateMembershipHandler : IQueryHandler<ValidateMembershipQuery, MembershipValidationResult>
{
    private readonly PlatformDbContext _db;
    private readonly IConnectionMultiplexer _redis;

    public ValidateMembershipHandler(PlatformDbContext db, IConnectionMultiplexer redis)
    {
        _db = db;
        _redis = redis;
    }

    public async Task<MembershipValidationResult> HandleAsync(ValidateMembershipQuery query, CancellationToken ct = default)
    {
        var cacheKey = $"membership:{query.UserId}:{query.TenantId}";
        var redisDb = _redis.GetDatabase();

        // Check cache
        var cached = await redisDb.StringGetAsync(cacheKey);
        if (!cached.IsNullOrEmpty)
        {
            var cachedResult = JsonSerializer.Deserialize<MembershipValidationResult>(cached!);
            if (cachedResult != null)
                return cachedResult;
        }

        // Query DB
        var membership = await _db.Memberships
            .Where(m => m.UserId == query.UserId && 
                        m.TenantId == query.TenantId && 
                        m.Status == MembershipStatus.Active)
            .Select(m => new MembershipValidationResult(true, m.Role.ToString()))
            .FirstOrDefaultAsync(ct);

        var result = membership ?? new MembershipValidationResult(false, null);

        // Cache for 5 minutes
        await redisDb.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), TimeSpan.FromMinutes(5));

        return result;
    }
}

