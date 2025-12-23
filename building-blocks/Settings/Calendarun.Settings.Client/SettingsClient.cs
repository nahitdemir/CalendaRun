using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Calendarun.Settings.Client;

public class SettingsClientOptions
{
    public string SettingsServiceUrl { get; set; } = "http://localhost:5301";
    public string RedisConnectionString { get; set; } = "localhost:6379";
    public string Environment { get; set; } = "dev";
    public TimeSpan L1CacheTtl { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan L2CacheTtl { get; set; } = TimeSpan.FromHours(2);
    public string[] WarmupKeys { get; set; } = Array.Empty<string>();
}

public class SettingsClient : ISettingsClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _memoryCache;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<SettingsClient> _logger;
    private readonly SettingsClientOptions _options;
    private static long _cacheHits;
    private static long _cacheMisses;

    public SettingsClient(
        HttpClient httpClient,
        IMemoryCache memoryCache,
        IConnectionMultiplexer redis,
        ILogger<SettingsClient> logger,
        SettingsClientOptions options)
    {
        _httpClient = httpClient;
        _memoryCache = memoryCache;
        _redis = redis;
        _logger = logger;
        _options = options;
    }

    public static (long hits, long misses) GetCacheStats() => (_cacheHits, _cacheMisses);

    private string GetL1CacheKey(string key, string? tenantId) => 
        $"settings:l1:{_options.Environment}:{tenantId ?? "global"}:{key}";

    private string GetL2HashKey(string? tenantId) => 
        $"settings:{_options.Environment}:{tenantId ?? "global"}";

    public async Task<T?> GetAsync<T>(string key, string? tenantId = null, CancellationToken ct = default)
    {
        var l1Key = GetL1CacheKey(key, tenantId);

        // L1: Memory Cache
        if (_memoryCache.TryGetValue<T>(l1Key, out var l1Value))
        {
            Interlocked.Increment(ref _cacheHits);
            _logger.LogDebug("L1 HIT: {Key}", key);
            return l1Value;
        }

        // L2: Redis
        try
        {
            var db = _redis.GetDatabase();
            var hashKey = GetL2HashKey(tenantId);
            var redisValue = await db.HashGetAsync(hashKey, key);
            
            if (redisValue.HasValue)
            {
                Interlocked.Increment(ref _cacheHits);
                _logger.LogDebug("L2 HIT: {Key}", key);
                var value = JsonSerializer.Deserialize<T>(redisValue.ToString());
                
                // Populate L1
                _memoryCache.Set(l1Key, value, _options.L1CacheTtl);
                return value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "L2 Redis read failed for {Key}", key);
        }

        // L3: HTTP to Settings Service
        Interlocked.Increment(ref _cacheMisses);
        _logger.LogDebug("Cache MISS, fetching from service: {Key}", key);

        try
        {
            var url = $"{_options.SettingsServiceUrl}/settings/{key}";
            if (tenantId != null)
                url += $"?tenantId={tenantId}";

            var response = await _httpClient.GetFromJsonAsync<SettingResponse>(url, ct);
            if (response != null)
            {
                var value = JsonSerializer.Deserialize<T>(response.Value.GetRawText());
                
                // Populate L1
                _memoryCache.Set(l1Key, value, _options.L1CacheTtl);
                
                _logger.LogDebug("L3 fetched and cached: {Key}", key);
                return value;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "L3 HTTP fetch failed for {Key}", key);
        }

        return default;
    }

    public async Task<IDictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, string? tenantId = null, CancellationToken ct = default)
    {
        var result = new Dictionary<string, T?>();
        var missingKeys = new List<string>();

        foreach (var key in keys)
        {
            var l1Key = GetL1CacheKey(key, tenantId);
            if (_memoryCache.TryGetValue<T>(l1Key, out var l1Value))
            {
                result[key] = l1Value;
                Interlocked.Increment(ref _cacheHits);
            }
            else
            {
                missingKeys.Add(key);
            }
        }

        if (missingKeys.Count == 0)
            return result;

        // Try L2 Redis for missing keys
        var stillMissingKeys = new List<string>();
        try
        {
            var db = _redis.GetDatabase();
            var hashKey = GetL2HashKey(tenantId);
            var redisKeys = missingKeys.Select(k => (RedisValue)k).ToArray();
            var values = await db.HashGetAsync(hashKey, redisKeys);

            for (int i = 0; i < missingKeys.Count; i++)
            {
                if (values[i].HasValue)
                {
                    var value = JsonSerializer.Deserialize<T>(values[i].ToString());
                    result[missingKeys[i]] = value;
                    _memoryCache.Set(GetL1CacheKey(missingKeys[i], tenantId), value, _options.L1CacheTtl);
                    Interlocked.Increment(ref _cacheHits);
                }
                else
                {
                    stillMissingKeys.Add(missingKeys[i]);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "L2 Redis bulk read failed");
            stillMissingKeys = missingKeys;
        }

        if (stillMissingKeys.Count == 0)
            return result;

        // L3: HTTP for still missing keys
        try
        {
            var keysParam = string.Join(",", stillMissingKeys);
            var url = $"{_options.SettingsServiceUrl}/settings?keys={keysParam}";
            if (tenantId != null)
                url += $"&tenantId={tenantId}";

            var response = await _httpClient.GetFromJsonAsync<Dictionary<string, JsonElement>>(url, ct);
            if (response != null)
            {
                foreach (var kv in response)
                {
                    var value = JsonSerializer.Deserialize<T>(kv.Value.GetRawText());
                    result[kv.Key] = value;
                    _memoryCache.Set(GetL1CacheKey(kv.Key, tenantId), value, _options.L1CacheTtl);
                    Interlocked.Increment(ref _cacheMisses);
                }
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "L3 HTTP bulk fetch failed");
        }

        return result;
    }

    public Task InvalidateAsync(string key, string? tenantId = null, CancellationToken ct = default)
    {
        var l1Key = GetL1CacheKey(key, tenantId);
        _memoryCache.Remove(l1Key);
        _logger.LogDebug("L1 invalidated: {Key}", key);
        return Task.CompletedTask;
    }

    public Task InvalidateAllAsync(string? tenantId = null, CancellationToken ct = default)
    {
        // MemoryCache doesn't support bulk invalidation by prefix easily
        // In production, use CacheEntryOptions with CancellationTokenSource for tag-based invalidation
        _logger.LogInformation("L1 full invalidation requested for tenant {TenantId}", tenantId ?? "global");
        return Task.CompletedTask;
    }

    private record SettingResponse(string Key, JsonElement Value, string TenantId);
}

