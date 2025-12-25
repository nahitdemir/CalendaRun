using Catalog.Application.Common;
using Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.Events.Queries;

public class GetAdminEventsHandler : IQueryHandler<GetAdminEventsQuery, Result<List<AdminEventDto>>>
{
    private readonly CatalogDbContext _db;
    private readonly ILogger<GetAdminEventsHandler> _logger;

    public GetAdminEventsHandler(CatalogDbContext db, ILogger<GetAdminEventsHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<List<AdminEventDto>>> HandleAsync(GetAdminEventsQuery query, CancellationToken ct = default)
    {
        var eventsQuery = _db.Events.AsQueryable();

        // Super admin sees ALL events, tenant admin sees only their tenant's events
        if (!query.IsSuperAdmin)
        {
            if (!query.TenantId.HasValue)
                return Result<List<AdminEventDto>>.Failure("Tenant ID is required");

            eventsQuery = eventsQuery.Where(e => e.TenantId == query.TenantId.Value);
        }

        var events = await eventsQuery
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new AdminEventDto(
                e.Id,
                e.TenantId,
                e.Title,
                e.Description,
                e.StartAt,
                e.City,
                e.CountryCode,
                e.RegistrationUrl,
                e.CreatedAt,
                e.CreatedBy,
                e.UpdatedAt,
                e.UpdatedBy,
                e.TenantId == null
            ))
            .ToListAsync(ct);

        _logger.LogInformation("Admin listed {Count} events (SuperAdmin={IsSuperAdmin})", events.Count, query.IsSuperAdmin);

        return Result<List<AdminEventDto>>.Success(events);
    }
}
