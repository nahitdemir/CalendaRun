using Catalog.Application.Common;

namespace Catalog.Application.Events.Queries;

public record GetEventsQuery(
    Guid? TenantId,
    string? City = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null,
    string? DistanceKm = null, // Comma-separated list of distances in KM (e.g., "5,10,21")
    int Page = 1,
    int PageSize = 20
) : IQuery<Result<EventsPagedResult>>;

public record EventsPagedResult(
    List<EventDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

public record EventDto(
    Guid Id,
    Guid? TenantId,
    string Title,
    string? Description,
    DateTimeOffset StartAt,
    string City,
    string CountryCode,
    string? RegistrationUrl,
    bool IsGlobal,
    string? Distances = null // Comma-separated list of distances in KM (converted from int[])
);

