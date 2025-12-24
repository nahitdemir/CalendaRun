using Catalog.Application.Common;
using Catalog.Domain;
using Catalog.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Catalog.Application.Events.Commands;

public class CreateEventHandler : ICommandHandler<CreateEventCommand, Result<CreateEventResult>>
{
    private readonly CatalogDbContext _db;
    private readonly ILogger<CreateEventHandler> _logger;

    public CreateEventHandler(CatalogDbContext db, ILogger<CreateEventHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<CreateEventResult>> HandleAsync(CreateEventCommand command, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            TenantId = command.IsGlobal && command.IsSuperAdmin ? null : command.TenantId,
            Title = command.Title,
            Description = command.Description,
            StartAt = command.StartAt,
            City = command.City,
            CountryCode = command.CountryCode,
            RegistrationUrl = command.RegistrationUrl ?? "",
            CreatedAt = now,
            CreatedBy = command.UserId
        };

        _db.Events.Add(eventEntity);

        // Add audit log
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = eventEntity.TenantId,
            ActorUserId = command.UserId,
            ActorEmail = command.UserEmail,
            Action = "EventCreated",
            EntityType = "Event",
            EntityId = eventEntity.Id,
            AfterJson = JsonSerializer.Serialize(eventEntity),
            TraceId = command.TraceId,
            Timestamp = now
        });

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Event created: {EventId} by {UserId} for tenant {TenantId}",
            eventEntity.Id, command.UserId, eventEntity.TenantId);

        return Result<CreateEventResult>.Success(new CreateEventResult(
            eventEntity.Id,
            eventEntity.TenantId,
            eventEntity.Title,
            eventEntity.StartAt
        ));
    }
}

