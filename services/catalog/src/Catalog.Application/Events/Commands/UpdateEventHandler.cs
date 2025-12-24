using Catalog.Application.Common;
using Catalog.Domain;
using Catalog.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Catalog.Application.Events.Commands;

public class UpdateEventHandler : ICommandHandler<UpdateEventCommand, Result<UpdateEventResult>>
{
    private readonly CatalogDbContext _db;
    private readonly ILogger<UpdateEventHandler> _logger;

    public UpdateEventHandler(CatalogDbContext db, ILogger<UpdateEventHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<UpdateEventResult>> HandleAsync(UpdateEventCommand command, CancellationToken ct = default)
    {
        var eventEntity = await _db.Events.FindAsync(new object[] { command.EventId }, ct);
        if (eventEntity == null)
            return Result<UpdateEventResult>.NotFound("Event not found");

        // Check tenant access
        if (!command.IsSuperAdmin)
        {
            if (!command.TenantId.HasValue)
                return Result<UpdateEventResult>.Failure("X-Tenant-Id header is required");

            if (eventEntity.TenantId != null && eventEntity.TenantId != command.TenantId)
                return Result<UpdateEventResult>.Forbidden();
        }

        var beforeJson = JsonSerializer.Serialize(eventEntity);
        var now = DateTimeOffset.UtcNow;

        eventEntity.Title = command.Title ?? eventEntity.Title;
        eventEntity.Description = command.Description ?? eventEntity.Description;
        eventEntity.StartAt = command.StartAt ?? eventEntity.StartAt;
        eventEntity.City = command.City ?? eventEntity.City;
        eventEntity.CountryCode = command.CountryCode ?? eventEntity.CountryCode;
        eventEntity.RegistrationUrl = command.RegistrationUrl ?? eventEntity.RegistrationUrl;
        eventEntity.UpdatedAt = now;
        eventEntity.UpdatedBy = command.UserId;

        // Add audit log
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = eventEntity.TenantId,
            ActorUserId = command.UserId,
            ActorEmail = command.UserEmail,
            Action = "EventUpdated",
            EntityType = "Event",
            EntityId = eventEntity.Id,
            BeforeJson = beforeJson,
            AfterJson = JsonSerializer.Serialize(eventEntity),
            TraceId = command.TraceId,
            Timestamp = now
        });

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Event updated: {EventId} by {UserId}", command.EventId, command.UserId);

        return Result<UpdateEventResult>.Success(new UpdateEventResult(
            eventEntity.Id,
            eventEntity.Title,
            eventEntity.UpdatedAt
        ));
    }
}

