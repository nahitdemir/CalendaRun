using Catalog.Application.Common;
using Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.Events.Queries;

public class GetEventByIdHandler : IQueryHandler<GetEventByIdQuery, Result<EventDto>>
{
    private readonly CatalogDbContext _db;
    private readonly ILogger<GetEventByIdHandler> _logger;

    public GetEventByIdHandler(CatalogDbContext db, ILogger<GetEventByIdHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<EventDto>> HandleAsync(GetEventByIdQuery query, CancellationToken ct = default)
    {
        var eventEntity = await _db.Events
            .Where(e => e.Id == query.EventId)
            .Select(e => new EventDto(
                e.Id,
                e.TenantId,
                e.Title,
                e.Description,
                e.StartAt,
                e.City,
                e.CountryCode,
                e.RegistrationUrl,
                e.TenantId == null
            ))
            .FirstOrDefaultAsync(ct);

        if (eventEntity == null)
        {
            _logger.LogWarning("Event not found: {EventId}", query.EventId);
            return Result<EventDto>.NotFound("Event not found");
        }

        // Check tenant access: if event is tenant-scoped, user must have access to that tenant
        // If event is global (TenantId == null), anyone can access it
        // If tenantId is provided in query, check if event belongs to that tenant or is global
        if (eventEntity.TenantId.HasValue)
        {
            // Event is tenant-scoped
            if (query.TenantId.HasValue && eventEntity.TenantId != query.TenantId)
            {
                _logger.LogWarning("User from tenant {QueryTenantId} tried to access event {EventId} from tenant {EventTenantId}",
                    query.TenantId, query.EventId, eventEntity.TenantId);
                return Result<EventDto>.Forbidden("You don't have access to this event");
            }
        }
        // If event is global (TenantId == null), allow access regardless of query.TenantId

        _logger.LogInformation("Retrieved event {EventId} for tenant {TenantId}", query.EventId, query.TenantId);

        return Result<EventDto>.Success(eventEntity);
    }
}

