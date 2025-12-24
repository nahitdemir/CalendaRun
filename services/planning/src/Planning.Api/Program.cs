using Calendarun.Contracts.Planning;
using Calendarun.Contracts.Settings;
using Calendarun.Settings.Client;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Planning.Api;
using Planning.Application;
using Planning.Infrastructure;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

var builder = WebApplication.CreateBuilder(args);

// ==================== LOGGING ====================
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProperty("Service", "Planning.Api")
    .WriteTo.Console(outputTemplate: 
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Service} {CorrelationId} {PlanItemId} {Message:lj}{NewLine}{Exception}",
        theme: AnsiConsoleTheme.Code)
    .CreateLogger();

builder.Host.UseSerilog();

// ==================== KESTREL ====================
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenLocalhost(5201);
});

// ==================== SERVICES ====================

// Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ==================== AUTHENTICATION ====================
var keycloakAuthority = builder.Configuration["Keycloak:Authority"] ?? "http://localhost:8180/realms/calendarun";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = keycloakAuthority;
        options.Audience = "calendarun-api";
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = keycloakAuthority,
            ValidateAudience = false,
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

// ==================== DATABASE ====================
var connectionString = builder.Configuration.GetConnectionString("PlanningDb");
builder.Services.AddDbContext<PlanningDbContext>(options =>
    options.UseNpgsql(connectionString));

// ==================== SETTINGS CLIENT ====================
builder.Services.AddSettingsClient(options =>
{
    options.SettingsServiceUrl = builder.Configuration["SettingsService:Url"] ?? "http://localhost:5301";
    options.RedisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.Environment = builder.Configuration["Environment"] ?? "dev";
    options.WarmupKeys = new[]
    {
        "planning.default_timezone",
        "planning.max_plans_per_user"
    };
});

// ==================== APPLICATION LAYER (CQRS) ====================
builder.Services.AddApplication();

// ==================== MASSTRANSIT ====================
builder.Services.AddMassTransit(x =>
{
    x.AddSettingsChangedConsumer("planning-api");

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.Message<PlanningUserPlannedV1>(m => m.SetEntityName("planning.userplanned.v1"));
        cfg.Message<SettingsChangedV1>(m => m.SetEntityName("settings.changed.v1"));

        cfg.ConfigureSettingsChangedEndpoint(context, "planning-api");
    });
});

// ==================== HEALTH CHECKS ====================
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString!, name: "postgres", tags: new[] { "db", "planning" })
    .AddRabbitMQ("amqp://guest:guest@localhost:5672", name: "rabbitmq", tags: new[] { "messaging" })
    .AddCheck<OutboxHealthCheck>("outbox", tags: new[] { "outbox", "planning" });

// ==================== BACKGROUND SERVICES ====================
builder.Services.AddHostedService<OutboxProcessor>();

// ==================== BUILD APP ====================
var app = builder.Build();

// ==================== MIDDLEWARE ====================
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ==================== ROUTES ====================
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
