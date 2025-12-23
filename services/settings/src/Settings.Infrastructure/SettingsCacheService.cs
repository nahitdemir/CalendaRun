using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Settings.Infrastructure;

public class SettingsCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<SettingsCacheService> _logger;
    private readonly string _environment;

    public SettingsCacheService(
        IConnectionMultiplexer redis, 
        ILogger<SettingsCacheService> logger,
        string environment = "dev")
    {
        _redis = redis;
        _logger = logger;
        _environment = environment;
    }

    private string GetHashKey(string? tenantId) => 
        $"settings:{_environment}:{tenantId ?? "global"}";
    
    private string GetVersionKey(string? tenantId) => 
        $"settings:version:{_environment}:{tenantId ?? "global"}";

    public async Task<string?> GetAsync(string key, string? tenantId, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var hashKey = GetHashKey(tenantId);
            
            var value = await db.HashGetAsync(hashKey, key);
            if (value.HasValue)
            {
                _logger.LogDebug("Cache HIT: {Key} for tenant {TenantId}", key, tenantId ?? "global");
                return value.ToString();
            }

            _logger.LogDebug("Cache MISS: {Key} for tenant {TenantId}", key, tenantId ?? "global");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache read failed for key {Key}", key);
            return null;
        }
    }

    public async Task<Dictionary<string, string>> GetManyAsync(IEnumerable<string> keys, string? tenantId, CancellationToken ct = default)
    {
        var result = new Dictionary<string, string>();
        try
        {
            var db = _redis.GetDatabase();
            var hashKey = GetHashKey(tenantId);
            var keyArray = keys.Select(k => (RedisValue)k).ToArray();
            
            var values = await db.HashGetAsync(hashKey, keyArray);
            var keyList = keys.ToList();
            
            for (int i = 0; i < keyList.Count; i++)
            {
                if (values[i].HasValue)
                {
                    result[keyList[i]] = values[i].ToString();
                }
            }
            
            _logger.LogDebug("Cache bulk get: {Count}/{Total} hits for tenant {TenantId}", 
                result.Count, keyList.Count, tenantId ?? "global");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache bulk read failed");
        }
        
        return result;
    }

    public async Task SetAsync(string key, string valueJson, string? tenantId, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var hashKey = GetHashKey(tenantId);
            
            await db.HashSetAsync(hashKey, key, valueJson);
            _logger.LogDebug("Cache SET: {Key} for tenant {TenantId}", key, tenantId ?? "global");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache write failed for key {Key}", key);
        }
    }

    public async Task SetManyAsync(Dictionary<string, string> settings, string? tenantId, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var hashKey = GetHashKey(tenantId);
            
            var entries = settings.Select(kv => new HashEntry(kv.Key, kv.Value)).ToArray();
            await db.HashSetAsync(hashKey, entries);
            
            _logger.LogDebug("Cache bulk SET: {Count} keys for tenant {TenantId}", 
                settings.Count, tenantId ?? "global");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache bulk write failed");
        }
    }

    public async Task SetVersionAsync(long version, string? tenantId, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var versionKey = GetVersionKey(tenantId);
            
            await db.StringSetAsync(versionKey, version.ToString());
            _logger.LogDebug("Cache version SET: {Version} for tenant {TenantId}", version, tenantId ?? "global");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache version write failed");
        }
    }

    public async Task<long?> GetVersionAsync(string? tenantId, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var versionKey = GetVersionKey(tenantId);
            
            var value = await db.StringGetAsync(versionKey);
            if (value.HasValue && long.TryParse(value.ToString(), out var version))
            {
                return version;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache version read failed");
        }
        
        return null;
    }

    public async Task InvalidateAsync(string key, string? tenantId, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var hashKey = GetHashKey(tenantId);
            
            await db.HashDeleteAsync(hashKey, key);
            _logger.LogDebug("Cache INVALIDATE: {Key} for tenant {TenantId}", key, tenantId ?? "global");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache invalidation failed for key {Key}", key);
        }
    }
}

