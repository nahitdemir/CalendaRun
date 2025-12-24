using Catalog.Application.Common;
using Catalog.Domain;
using Catalog.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Catalog.Application.Events.Commands;

public class DeleteEventHandler : ICommandHandler<DeleteEventCommand, Result>
{
    private readonly CatalogDbContext _db;
    private readonly ILogger<DeleteEventHandler> _logger;

    public DeleteEventHandler(CatalogDbContext db, ILogger<DeleteEventHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(DeleteEventCommand command, CancellationToken ct = default)
    {
        var eventEntity = await _db.Events.FindAsync(new object[] { command.EventId }, ct);
        if (eventEntity == null)
            return Result.NotFound("Event not found");

        // Check tenant access
        if (!command.IsSuperAdmin)
        {
            if (!command.TenantId.HasValue)
                return Result.Failure("X-Tenant-Id header is required");

            if (eventEntity.TenantId != command.TenantId)
                return Result.Forbidden();
        }

        var beforeJson = JsonSerializer.Serialize(eventEntity);
        var now = DateTimeOffset.UtcNow;

        _db.Events.Remove(eventEntity);

        // Add audit log
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = eventEntity.TenantId,
            ActorUserId = command.UserId,
            ActorEmail = command.UserEmail,
            Action = "EventDeleted",
            EntityType = "Event",
            EntityId = eventEntity.Id,
            BeforeJson = beforeJson,
            TraceId = command.TraceId,
            Timestamp = now
        });

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Event deleted: {EventId} by {UserId}", command.EventId, command.UserId);

        return Result.Success();
    }
}

