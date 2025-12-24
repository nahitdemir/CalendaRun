using Catalog.Application.Common;
using Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.Events.Queries;

public class GetEventsHandler : IQueryHandler<GetEventsQuery, Result<List<EventDto>>>
{
    private readonly CatalogDbContext _db;
    private readonly ILogger<GetEventsHandler> _logger;

    public GetEventsHandler(CatalogDbContext db, ILogger<GetEventsHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<List<EventDto>>> HandleAsync(GetEventsQuery query, CancellationToken ct = default)
    {
        var eventsQuery = _db.Events.AsQueryable();

        if (query.TenantId.HasValue)
        {
            // Show global events + tenant's events
            eventsQuery = eventsQuery.Where(e => e.TenantId == null || e.TenantId == query.TenantId.Value);
        }
        else
        {
            // No tenant context - show only global events
            eventsQuery = eventsQuery.Where(e => e.TenantId == null);
        }

        var events = await eventsQuery
            .OrderBy(e => e.StartAt)
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
            .ToListAsync(ct);

        _logger.LogInformation("Retrieved {Count} events for tenant {TenantId}", events.Count, query.TenantId);

        return Result<List<EventDto>>.Success(events);
    }
}

