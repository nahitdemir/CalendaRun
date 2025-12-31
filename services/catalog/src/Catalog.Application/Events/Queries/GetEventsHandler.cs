using Catalog.Application.Common;
using Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.Events.Queries;

public class GetEventsHandler : IQueryHandler<GetEventsQuery, Result<EventsPagedResult>>
{
    private readonly CatalogDbContext _db;
    private readonly ILogger<GetEventsHandler> _logger;

    public GetEventsHandler(CatalogDbContext db, ILogger<GetEventsHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<EventsPagedResult>> HandleAsync(GetEventsQuery query, CancellationToken ct = default)
    {
        var eventsQuery = _db.Events.AsQueryable();

        // Tenant filtering
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

        // City filter (exact match, case-insensitive)
        if (!string.IsNullOrWhiteSpace(query.City))
        {
            // Use ILIKE for case-insensitive exact match in PostgreSQL
            // ILIKE is more reliable than ToLower() in EF Core for PostgreSQL
            var cityValue = query.City.Trim();
            _logger.LogDebug("Filtering by city: '{City}' (trimmed: '{CityTrimmed}')", query.City, cityValue);
            eventsQuery = eventsQuery.Where(e => e.City != null && EF.Functions.ILike(e.City, cityValue));
        }

        // Date range filter
        // IMPORTANT: All DateTimeOffset comparisons must use UTC (offset 0) for PostgreSQL timestamptz
        // Filter: StartAt >= fromUtc AND StartAt < toExclusiveUtc
        // Controller already parses dates and converts to UTC with offset 0, so we can use them directly
        if (query.DateFrom.HasValue)
        {
            // query.DateFrom already has UTC offset 0 from controller
            // Ensure we use it with offset 0 for PostgreSQL timestamptz compatibility
            var dateFromUtcOffset = new DateTimeOffset(query.DateFrom.Value.UtcDateTime, TimeSpan.Zero);
            eventsQuery = eventsQuery.Where(e => e.StartAt >= dateFromUtcOffset);
            
            #if DEBUG
            _logger.LogDebug("Date filter (from): StartAt >= {DateFromUtc} (UTC offset 0)", dateFromUtcOffset);
            #endif
        }

        if (query.DateTo.HasValue)
        {
            // query.DateTo already has UTC offset 0 and is already set to next day start (exclusive) from controller
            // Don't use .Date property as it may use local timezone - use UtcDateTime directly
            var dateToUtcOffset = new DateTimeOffset(query.DateTo.Value.UtcDateTime, TimeSpan.Zero);
            eventsQuery = eventsQuery.Where(e => e.StartAt < dateToUtcOffset);
            
            #if DEBUG
            _logger.LogDebug("Date filter (to): StartAt < {DateToUtc} (UTC offset 0, exclusive)", dateToUtcOffset);
            #endif
        }

        // Distance filter (if DistanceKm is provided, parse comma-separated values)
        if (!string.IsNullOrWhiteSpace(query.DistanceKm))
        {
            var requestedDistances = query.DistanceKm
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(d => d.Trim())
                .Where(d => !string.IsNullOrEmpty(d))
                .Select(d => int.TryParse(d, out var dist) ? dist : (int?)null)
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .ToList();

            if (requestedDistances.Count > 0)
            {
                // Filter events where Distances array contains any of the requested distances
                // Distances is stored as PostgreSQL integer[] (e.g., [5, 10, 21, 42])
                // Very simple query with array contains check
                eventsQuery = eventsQuery.Where(e =>
                    e.Distances != null &&
                    e.Distances.Any(d => requestedDistances.Contains(d))
                );
            }
        }

        // Get total count before pagination
        var totalCount = await eventsQuery.CountAsync(ct);

        // Pagination
        var events = await eventsQuery
            .OrderBy(e => e.StartAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(e => new EventDto(
                e.Id,
                e.TenantId,
                e.Title,
                e.Description,
                e.StartAt,
                e.City,
                e.CountryCode,
                e.RegistrationUrl,
                e.TenantId == null,
                e.Distances != null ? string.Join(",", e.Distances) : null
            ))
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);

        _logger.LogInformation(
            "Retrieved {Count} events (page {Page}, pageSize {PageSize}, total {Total}) for tenant {TenantId}",
            events.Count, query.Page, query.PageSize, totalCount, query.TenantId);

        return Result<EventsPagedResult>.Success(new EventsPagedResult(
            events,
            totalCount,
            query.Page,
            query.PageSize,
            totalPages
        ));
    }
}

