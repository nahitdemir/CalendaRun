using Calendarun.Common.Auth;
using Calendarun.Contracts.Settings;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Settings.Infrastructure;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenLocalhost(5301);
});

// Add services
builder.Services.AddControllers();
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

// ==================== AUTHENTICATION ====================
var keycloakAuthority = builder.Configuration["Keycloak:Authority"] ?? "http://localhost:8180/realms/calendarun";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = keycloakAuthority;
        options.Audience = "calendarun-api";
        options.RequireHttpsMetadata = false; // Dev only
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = keycloakAuthority,
            ValidateAudience = false, // Gateway already validates audience
            ValidateLifetime = true,
            NameClaimType = CalendarunClaimTypes.PreferredUsername,
            RoleClaimType = CalendarunClaimTypes.RealmRoles
        };
    });

builder.Services.AddAuthorization();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
