using System.Text.Json;
using Calendarun.Contracts.Planning;
using Calendarun.Contracts.Settings;
using Calendarun.Settings.Client;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Planning.Api;
using Planning.Domain;
using Planning.Infrastructure;
using Serilog;
using SerilogContext = Serilog.Context.LogContext;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProperty("Service", "Planning.Api")
    .WriteTo.Console(outputTemplate: 
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Service} {CorrelationId} {PlanItemId} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Configure Kestrel
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenLocalhost(5201);
});

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Settings Client
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

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("PlanningDb");
builder.Services.AddDbContext<PlanningDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add MassTransit
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

        // Set entity name for message routing
        cfg.Message<PlanningUserPlannedV1>(m => m.SetEntityName("planning.userplanned.v1"));
        cfg.Message<SettingsChangedV1>(m => m.SetEntityName("settings.changed.v1"));

        cfg.ConfigureSettingsChangedEndpoint(context, "planning-api");
    });
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString!, name: "postgres", tags: new[] { "db", "planning" })
    .AddRabbitMQ("amqp://guest:guest@localhost:5672", name: "rabbitmq", tags: new[] { "messaging" })
    .AddCheck<OutboxHealthCheck>("outbox", tags: new[] { "outbox", "planning" });

// Add outbox processor
builder.Services.AddHostedService<Planning.Api.OutboxProcessor>();

var app = builder.Build();

// Add CorrelationId middleware
app.UseMiddleware<CorrelationIdMiddleware>();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Health endpoint
app.MapHealthChecks("/health");

// Plan endpoint
app.MapPost("/plan", async (
    CreatePlanRequest request,
    PlanningDbContext db,
    ISettingsClient settingsClient,
    HttpContext httpContext,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    // Get or create user from X-Dev-User header
    var userEmail = httpContext.Request.Headers["X-Dev-User"].FirstOrDefault() ?? "dev@local";
    
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == userEmail, ct);
    if (user == null)
    {
        user = new User
        {
            Id = Guid.NewGuid(),
            Email = userEmail,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
    }

    // Check max plans per user (from settings)
    var maxPlans = await settingsClient.GetAsync<int?>("planning.max_plans_per_user", null, ct) ?? 100;
    var currentPlanCount = await db.UserPlanItems.CountAsync(p => p.UserId == user.Id && p.State == "Active", ct);
    
    if (currentPlanCount >= maxPlans)
    {
        return Results.BadRequest(new { error = "Maximum plan limit reached", maxPlans, currentCount = currentPlanCount });
    }

    // Check if already planned
    var existingPlan = await db.UserPlanItems
        .FirstOrDefaultAsync(p => p.UserId == user.Id && p.EventId == request.EventId, ct);
    
    if (existingPlan != null)
    {
        return Results.Conflict(new { error = "Event already planned", planItemId = existingPlan.Id });
    }

    // Get timezone from settings
    var timezone = await settingsClient.GetAsync<string>("planning.default_timezone", null, ct) ?? "Europe/Istanbul";

    // Create plan item
    var planItem = new UserPlanItem
    {
        Id = Guid.NewGuid(),
        UserId = user.Id,
        EventId = request.EventId,
        State = "Active",
        CreatedAt = DateTimeOffset.UtcNow
    };

    db.UserPlanItems.Add(planItem);
    
    // Enrich logs with PlanItemId
    using (SerilogContext.PushProperty("PlanItemId", planItem.Id))
    {
    
    // Add to outbox (transactional)
    var eventMessage = new PlanningUserPlannedV1(
        user.Id,
        user.Email,
        request.EventId,
        planItem.Id,
        timezone, // From settings
        DateTimeOffset.UtcNow
    );

    var outboxMessage = new OutboxMessage
    {
        Id = Guid.NewGuid(),
        Type = nameof(PlanningUserPlannedV1),
        Payload = JsonSerializer.Serialize(eventMessage),
        CreatedAt = DateTimeOffset.UtcNow
    };

    db.OutboxMessages.Add(outboxMessage);
    
    logger.LogInformation("💾 Writing to outbox... UserId={UserId} EventId={EventId} PlanItemId={PlanItemId} Timezone={Timezone}",
        user.Id, request.EventId, planItem.Id, timezone);

    await db.SaveChangesAsync(ct);
    
    logger.LogInformation("✅ Saved to outbox (will be published by background worker)");
    
    } // End LogContext

    return Results.Created($"/plan/{planItem.Id}", new
    {
        planItemId = planItem.Id,
        userId = user.Id,
        eventId = request.EventId,
        state = planItem.State,
        createdAt = planItem.CreatedAt,
        timezone
    });
})
.WithName("CreatePlan")
.WithOpenApi();

app.Run();

public record CreatePlanRequest(Guid EventId);
