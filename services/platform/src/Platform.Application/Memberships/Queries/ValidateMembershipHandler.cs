using Calendarun.Settings.Client;
using Microsoft.EntityFrameworkCore;
using Platform.Application.Common;
using Platform.Domain;
using Platform.Domain.Entities;
using Platform.Infrastructure.Data;
using StackExchange.Redis;
using System.Text.Json;

namespace Platform.Application.Memberships.Queries;

public class ValidateMembershipHandler : IQueryHandler<ValidateMembershipQuery, MembershipValidationResult>
{
    private readonly PlatformDbContext _db;
    private readonly IConnectionMultiplexer _redis;
    private readonly ISettingsClient _settingsClient;

    public ValidateMembershipHandler(
        PlatformDbContext db,
        IConnectionMultiplexer redis,
        ISettingsClient settingsClient)
    {
        _db = db;
        _redis = redis;
        _settingsClient = settingsClient;
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

        // Get cache TTL from Settings (tenant-specific or global)
        var tenantIdStr = query.TenantId.ToString();
        var cacheTtlMinutes = await _settingsClient.GetAsync<int?>(
            PlatformDefaults.SettingsKeys.MembershipCacheTtlMinutes, tenantIdStr, ct)
            ?? PlatformDefaults.DefaultMembershipCacheTtlMinutes;

        // Cache with TTL from Settings
        await redisDb.StringSetAsync(
            cacheKey,
            JsonSerializer.Serialize(result),
            TimeSpan.FromMinutes(cacheTtlMinutes));

        return result;
    }
}

