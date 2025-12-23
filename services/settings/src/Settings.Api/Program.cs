using System.Text.Json;
using Calendarun.Contracts.Settings;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Settings.Domain;
using Settings.Infrastructure;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenLocalhost(5301);
});

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("SettingsDb");
builder.Services.AddDbContext<SettingsDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add Redis
var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnection));
builder.Services.AddSingleton<SettingsCacheService>(sp =>
{
    var redis = sp.GetRequiredService<IConnectionMultiplexer>();
    var logger = sp.GetRequiredService<ILogger<SettingsCacheService>>();
    var env = builder.Configuration["Environment"] ?? "dev";
    return new SettingsCacheService(redis, logger, env);
});

// Add MassTransit
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
        cfg.Message<SettingsChangedV1>(m => m.SetEntityName("settings.changed.v1"));
    });
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString!, name: "postgres", tags: new[] { "db", "settings" })
    .AddRedis(redisConnection, name: "redis", tags: new[] { "cache" });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");

// GET /settings?keys=a,b,c&tenantId=...
app.MapGet("/settings", async (
    string? keys,
    string? tenantId,
    SettingsDbContext db,
    SettingsCacheService cache,
    CancellationToken ct) =>
{
    var keyList = keys?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
    var environment = "dev";
    var result = new Dictionary<string, object?>();

    foreach (var key in keyList)
    {
        var resolved = await ResolveSettingAsync(db, cache, key, tenantId, environment, ct);
        result[key] = resolved;
    }

    return Results.Ok(result);
})
.WithName("GetSettings")
.WithOpenApi();

// GET /settings/{key}?tenantId=...
app.MapGet("/settings/{key}", async (
    string key,
    string? tenantId,
    SettingsDbContext db,
    SettingsCacheService cache,
    CancellationToken ct) =>
{
    var environment = "dev";
    var resolved = await ResolveSettingAsync(db, cache, key, tenantId, environment, ct);
    
    if (resolved == null)
        return Results.NotFound(new { error = "Setting not found", key });

    return Results.Ok(new { key, value = resolved, tenantId = tenantId ?? "global" });
})
.WithName("GetSetting")
.WithOpenApi();

// PUT /settings/{key}?tenantId=...
app.MapPut("/settings/{key}", async (
    string key,
    string? tenantId,
    UpdateSettingRequest request,
    SettingsDbContext db,
    SettingsCacheService cache,
    IPublishEndpoint publishEndpoint,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    var environment = "dev";
    
    // Check if definition exists
    var definition = await db.SettingDefinitions.FindAsync(new object[] { key }, ct);
    if (definition == null)
        return Results.NotFound(new { error = "Setting definition not found", key });

    // Find existing value
    var existing = await db.SettingValues
        .FirstOrDefaultAsync(v => v.Key == key && v.TenantId == tenantId && v.Environment == environment, ct);

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
            Environment = environment,
            ValueJson = request.ValueJson,
            Version = newVersion,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.SettingValues.Add(existing);
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
    db.SettingAudits.Add(audit);

    await db.SaveChangesAsync(ct);

    // Update cache (write-through)
    await cache.SetAsync(key, request.ValueJson, tenantId, ct);
    await cache.SetVersionAsync(newVersion, tenantId, ct);

    // Publish event
    logger.LogInformation("📢 Publishing SettingsChangedV1 for key {Key}", key);
    await publishEndpoint.Publish(new SettingsChangedV1(
        tenantId,
        new[] { key },
        newVersion,
        DateTimeOffset.UtcNow
    ), ct);

    logger.LogInformation("✅ Setting updated: {Key} v{Version}", key, newVersion);

    return Results.Ok(new { key, version = newVersion, tenantId = tenantId ?? "global" });
})
.WithName("UpdateSetting")
.WithOpenApi();

// GET /settings/version?tenantId=...
app.MapGet("/settings/version", async (
    string? tenantId,
    SettingsDbContext db,
    SettingsCacheService cache,
    CancellationToken ct) =>
{
    var environment = "dev";
    
    // Try cache first
    var cachedVersion = await cache.GetVersionAsync(tenantId, ct);
    if (cachedVersion.HasValue)
        return Results.Ok(new { version = cachedVersion.Value, source = "cache" });

    // Fall back to DB
    var maxVersion = await db.SettingValues
        .Where(v => v.TenantId == tenantId && v.Environment == environment)
        .MaxAsync(v => (long?)v.Version, ct) ?? 0;

    return Results.Ok(new { version = maxVersion, source = "db" });
})
.WithName("GetSettingsVersion")
.WithOpenApi();

app.Run();

// Helper methods
static async Task<object?> ResolveSettingAsync(
    SettingsDbContext db,
    SettingsCacheService cache,
    string key,
    string? tenantId,
    string environment,
    CancellationToken ct)
{
    // Try cache first (tenant-specific)
    if (tenantId != null)
    {
        var cached = await cache.GetAsync(key, tenantId, ct);
        if (cached != null)
            return JsonSerializer.Deserialize<object>(cached);
    }

    // Try cache (global)
    var globalCached = await cache.GetAsync(key, null, ct);
    if (globalCached != null && tenantId == null)
        return JsonSerializer.Deserialize<object>(globalCached);

    // Fall back to DB - tenant-specific first
    SettingValue? value = null;
    if (tenantId != null)
    {
        value = await db.SettingValues
            .FirstOrDefaultAsync(v => v.Key == key && v.TenantId == tenantId && v.Environment == environment, ct);
    }

    // Fall back to global
    if (value == null)
    {
        value = await db.SettingValues
            .FirstOrDefaultAsync(v => v.Key == key && v.TenantId == null && v.Environment == environment, ct);
    }

    if (value == null) return null;

    // Populate cache
    await cache.SetAsync(key, value.ValueJson, value.TenantId, ct);

    return JsonSerializer.Deserialize<object>(value.ValueJson);
}

public record UpdateSettingRequest(string ValueJson, string? UpdatedBy);
