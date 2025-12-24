using Catalog.Application.Common;

namespace Catalog.Application.Events.Queries;

public record GetEventsQuery(Guid? TenantId) : IQuery<Result<List<EventDto>>>;

public record EventDto(
    Guid Id,
    Guid? TenantId,
    string Title,
    string? Description,
    DateTimeOffset StartAt,
    string City,
    string CountryCode,
    string? RegistrationUrl,
    bool IsGlobal
);

