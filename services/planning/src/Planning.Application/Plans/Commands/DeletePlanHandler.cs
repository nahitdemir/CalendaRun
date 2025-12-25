using Microsoft.Extensions.Logging;
using Planning.Application.Common;
using Planning.Domain;
using Planning.Infrastructure;
using System.Text.Json;

namespace Planning.Application.Plans.Commands;

public class DeletePlanHandler : ICommandHandler<DeletePlanCommand, Result>
{
    private readonly PlanningDbContext _db;
    private readonly ILogger<DeletePlanHandler> _logger;

    public DeletePlanHandler(PlanningDbContext db, ILogger<DeletePlanHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(DeletePlanCommand command, CancellationToken ct = default)
    {
        var planItem = await _db.UserPlanItems.FindAsync(new object[] { command.PlanId }, ct);
        if (planItem == null)
            return Result.NotFound("Plan not found");

        // Tenant admin can only delete their own tenant's plans
        if (!command.IsSuperAdmin)
        {
            if (!command.TenantId.HasValue)
                return Result.Failure("Tenant ID is required");

            if (planItem.TenantId != command.TenantId)
                return Result.Forbidden();
        }

        var beforeJson = JsonSerializer.Serialize(new 
        { 
            planItem.Id, 
            planItem.UserId, 
            planItem.EventId, 
            planItem.State 
        });
        
        var now = DateTimeOffset.UtcNow;

        _db.UserPlanItems.Remove(planItem);

        // Add audit log
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = planItem.TenantId,
            ActorUserId = command.UserId,
            ActorEmail = command.UserEmail,
            Action = "PlanDeleted",
            EntityType = "UserPlanItem",
            EntityId = planItem.Id,
            BeforeJson = beforeJson,
            TraceId = command.TraceId,
            Timestamp = now
        });

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Plan deleted: {PlanId} by {UserId}", command.PlanId, command.UserId);

        return Result.Success();
    }
}
