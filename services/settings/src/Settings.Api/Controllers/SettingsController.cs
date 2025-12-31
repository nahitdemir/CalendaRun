using System.Text.Json;
using Calendarun.Contracts.Settings;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Settings.Domain;
using Settings.Infrastructure;

namespace Settings.Api.Controllers;

[ApiController]
[Route("settings")]
public class SettingsController : ControllerBase
{
    private readonly SettingsDbContext _db;
    private readonly SettingsCacheService _cache;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<SettingsController> _logger;
    private const string Environment = "dev";

    public SettingsController(
        SettingsDbContext db,
        SettingsCacheService cache,
        IPublishEndpoint publishEndpoint,
        ILogger<SettingsController> logger)
    {
        _db = db;
        _cache = cache;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    /// <summary>
    /// Get multiple settings by keys
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSettings(
        [FromQuery] string? keys,
        [FromQuery] string? tenantId,
        CancellationToken ct)
    {
        var keyList = keys?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        var result = new Dictionary<string, object?>();

        foreach (var key in keyList)
        {
            var resolved = await ResolveSettingAsync(key, tenantId, ct);
            result[key] = resolved;
        }

        return Ok(result);
    }

    /// <summary>
    /// Get a single setting by key
    /// Note: {**key} allows dots in the key (e.g., "event.distances")
    /// tenantId is optional - if not provided, returns global setting
    /// Public endpoint - no authentication required
    /// </summary>
    [HttpGet("{**key}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSetting(
        string key,
        [FromQuery] string? tenantId,
        CancellationToken ct)
    {
        // tenantId is optional - if null, ResolveSettingAsync will fall back to global setting
        var resolved = await ResolveSettingAsync(key, tenantId, ct);
        
        if (resolved == null)
            return NotFound(new { error = "Setting not found", key });

        return Ok(new { key, value = resolved, tenantId = tenantId ?? "global" });
    }

    /// <summary>
    /// Update a setting
    /// </summary>
    [HttpPut("{key}")]
    public async Task<IActionResult> UpdateSetting(
        string key,
        [FromQuery] string? tenantId,
        [FromBody] UpdateSettingRequest request,
        CancellationToken ct)
    {
        // Check if definition exists
        var definition = await _db.SettingDefinitions.FindAsync(new object[] { key }, ct);
        if (definition == null)
            return NotFound(new { error = "Setting definition not found", key });

        // Find existing value
        var existing = await _db.SettingValues
            .FirstOrDefaultAsync(v => v.Key == key && v.TenantId == tenantId && v.Environment == Environment, ct);

        string? oldValueJson = existing?.ValueJson;
        long newVersion;
        
        if (existing != null)
        {
            // Update
            existing.ValueJson = request.ValueJson;
            existing.Version++;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            newVersion = existing.Version;
        }
        else
        {
            // Insert
            newVersion = 1;
            existing = new SettingValue
            {
                Id = Guid.NewGuid(),
                Key = key,
                TenantId = tenantId,
                Environment = Environment,
                ValueJson = request.ValueJson,
                Version = newVersion,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _db.SettingValues.Add(existing);
        }

        // Add audit
        var audit = new SettingAudit
        {
            Id = Guid.NewGuid(),
            Key = key,
            TenantId = tenantId,
            OldValueJson = oldValueJson,
            NewValueJson = request.ValueJson,
            UpdatedBy = request.UpdatedBy ?? "system",
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _db.SettingAudits.Add(audit);

        await _db.SaveChangesAsync(ct);

        // Update cache (write-through)
        await _cache.SetAsync(key, request.ValueJson, tenantId, ct);
        await _cache.SetVersionAsync(newVersion, tenantId, ct);

        // Publish event
        _logger.LogInformation("📢 Publishing SettingsChangedV1 for key {Key}", key);
        await _publishEndpoint.Publish(new SettingsChangedV1(
            tenantId,
            new[] { key },
            newVersion,
            DateTimeOffset.UtcNow
        ), ct);

        _logger.LogInformation("✅ Setting updated: {Key} v{Version}", key, newVersion);

        return Ok(new { key, version = newVersion, tenantId = tenantId ?? "global" });
    }

    /// <summary>
    /// Get settings version (for cache invalidation)
    /// </summary>
    [HttpGet("version")]
    public async Task<IActionResult> GetSettingsVersion(
        [FromQuery] string? tenantId,
        CancellationToken ct)
    {
        // Try cache first
        var cachedVersion = await _cache.GetVersionAsync(tenantId, ct);
        if (cachedVersion.HasValue)
            return Ok(new { version = cachedVersion.Value, source = "cache" });

        // Fall back to DB
        var maxVersion = await _db.SettingValues
            .Where(v => v.TenantId == tenantId && v.Environment == Environment)
            .MaxAsync(v => (long?)v.Version, ct) ?? 0;

        return Ok(new { version = maxVersion, source = "db" });
    }

    // Helper method
    private async Task<object?> ResolveSettingAsync(
        string key,
        string? tenantId,
        CancellationToken ct)
    {
        // Try cache first (tenant-specific)
        if (tenantId != null)
        {
            var cached = await _cache.GetAsync(key, tenantId, ct);
            if (cached != null)
                return JsonSerializer.Deserialize<object>(cached);
        }

        // Try cache (global)
        var globalCached = await _cache.GetAsync(key, null, ct);
        if (globalCached != null && tenantId == null)
            return JsonSerializer.Deserialize<object>(globalCached);

        // Fall back to DB - tenant-specific first
        SettingValue? value = null;
        if (tenantId != null)
        {
            value = await _db.SettingValues
                .FirstOrDefaultAsync(v => v.Key == key && v.TenantId == tenantId && v.Environment == Environment, ct);
        }

        // Fall back to global
        if (value == null)
        {
            value = await _db.SettingValues
                .FirstOrDefaultAsync(v => v.Key == key && v.TenantId == null && v.Environment == Environment, ct);
        }

        if (value == null) return null;

        // Populate cache
        await _cache.SetAsync(key, value.ValueJson, value.TenantId, ct);

        return JsonSerializer.Deserialize<object>(value.ValueJson);
    }
}

public record UpdateSettingRequest(string ValueJson, string? UpdatedBy);

