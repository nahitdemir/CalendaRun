using Calendarun.Common.Auth;
using Calendarun.Settings.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Platform.Application;
using Platform.Infrastructure.Data;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ==================== LOGGING ====================
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "Platform.Api")
    .WriteTo.Console(theme: AnsiConsoleTheme.Code)
    .CreateLogger();

builder.Host.UseSerilog();

// ==================== SERVICES ====================

// Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
var connectionString = builder.Configuration.GetConnectionString("PlatformDb");
builder.Services.AddDbContext<PlatformDbContext>(options =>
    options.UseNpgsql(connectionString));

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));

// Settings Client
builder.Services.AddSettingsClient(options =>
{
    options.SettingsServiceUrl = builder.Configuration["SettingsService:Url"] ?? "http://localhost:5301";
    options.RedisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.Environment = builder.Environment.EnvironmentName.ToLower();
});

// Application Layer (CQRS Handlers)
builder.Services.AddApplication();

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
            ValidateAudience = false,
            ValidateLifetime = true,
            NameClaimType = CalendarunClaimTypes.PreferredUsername,
            RoleClaimType = CalendarunClaimTypes.RealmRoles
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdmin", policy =>
        policy.RequireClaim(CalendarunClaimTypes.RealmRoles, Roles.SuperAdmin));
});

// ==================== HEALTH CHECKS ====================
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString!, name: "postgres-platform-db");

// ==================== BUILD APP ====================
var app = builder.Build();

// ==================== MIDDLEWARE ====================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// ==================== ROUTES ====================
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
