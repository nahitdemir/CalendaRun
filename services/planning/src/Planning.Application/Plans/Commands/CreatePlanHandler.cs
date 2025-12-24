using Calendarun.Common;
using Calendarun.Contracts.Planning;
using Calendarun.Settings.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Planning.Application.Common;
using Planning.Domain;
using Planning.Infrastructure;
using System.Text.Json;

namespace Planning.Application.Plans.Commands;

public class CreatePlanHandler : ICommandHandler<CreatePlanCommand, Result<CreatePlanResult>>
{
    private readonly PlanningDbContext _db;
    private readonly ISettingsClient _settingsClient;
    private readonly ILogger<CreatePlanHandler> _logger;

    public CreatePlanHandler(
        PlanningDbContext db, 
        ISettingsClient settingsClient,
        ILogger<CreatePlanHandler> logger)
    {
        _db = db;
        _settingsClient = settingsClient;
        _logger = logger;
    }

    public async Task<Result<CreatePlanResult>> HandleAsync(CreatePlanCommand command, CancellationToken ct = default)
    {
        // Get or create user within tenant
        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.TenantId == command.TenantId && u.Email == command.UserEmail, ct);
        
        if (user == null)
        {
            user = new User
            {
                Id = command.UserId,
                TenantId = command.TenantId,
                Email = command.UserEmail,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Created user {UserId} in tenant {TenantId}", user.Id, command.TenantId);
        }

        // Check max plans per user (from settings, tenant-aware if tenant provided)
        var tenantIdStr = command.TenantId?.ToString() ?? PlanningDefaults.GlobalSettingsKey;
        var maxPlans = await _settingsClient.GetAsync<int?>(
            PlanningDefaults.SettingsKeys.MaxPlansPerUser, tenantIdStr, ct) ?? PlanningDefaults.DefaultMaxPlansPerUser;
        
        var currentPlanCount = await _db.UserPlanItems.CountAsync(
            p => p.TenantId == command.TenantId && p.UserId == user.Id && p.State == PlanState.Active, ct);

        if (currentPlanCount >= maxPlans)
        {
            return Result<CreatePlanResult>.Failure(
                $"Maximum plan limit reached. Max: {maxPlans}, Current: {currentPlanCount}");
        }

        // Check if already planned (within tenant)
        var existingPlan = await _db.UserPlanItems
            .FirstOrDefaultAsync(p => p.TenantId == command.TenantId && 
                                       p.UserId == user.Id && 
                                       p.EventId == command.EventId, ct);

        if (existingPlan != null)
        {
            return Result<CreatePlanResult>.Conflict(
                $"Event already planned. PlanItemId: {existingPlan.Id}");
        }

        // Get timezone from settings (planning-specific, fallback to default)
        var timezone = await _settingsClient.GetAsync<string>(
            PlanningDefaults.SettingsKeys.DefaultTimezone, tenantIdStr, ct) ?? PlanningDefaults.DefaultTimezone;

        // Create plan item
        var planItem = new UserPlanItem
        {
            Id = Guid.NewGuid(),
            TenantId = command.TenantId,
            UserId = user.Id,
            EventId = command.EventId,
            State = PlanState.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = command.UserId
        };

        _db.UserPlanItems.Add(planItem);

        // Add to outbox (transactional)
        var eventMessage = new PlanningUserPlannedV1(
            user.Id,
            user.Email,
            command.EventId,
            planItem.Id,
            timezone,
            DateTimeOffset.UtcNow,
            command.TenantId
        );

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(PlanningUserPlannedV1),
            Payload = JsonSerializer.Serialize(eventMessage),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.OutboxMessages.Add(outboxMessage);

        // Add audit log
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = command.TenantId,
            ActorUserId = command.UserId,
            ActorEmail = command.UserEmail,
            Action = "PlanCreated",
            EntityType = "UserPlanItem",
            EntityId = planItem.Id,
            AfterJson = JsonSerializer.Serialize(new { planItem.Id, planItem.EventId, planItem.UserId }),
            TraceId = command.TraceId,
            Timestamp = DateTimeOffset.UtcNow
        });

        _logger.LogInformation(
            "💾 Writing to outbox... UserId={UserId} EventId={EventId} PlanItemId={PlanItemId} TenantId={TenantId}",
            user.Id, command.EventId, planItem.Id, command.TenantId);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("✅ Saved to outbox (will be published by background worker)");

        return Result<CreatePlanResult>.Success(new CreatePlanResult(
            planItem.Id,
            command.TenantId,
            user.Id,
            command.EventId,
            planItem.State,
            planItem.CreatedAt,
            timezone
        ));
    }
}

