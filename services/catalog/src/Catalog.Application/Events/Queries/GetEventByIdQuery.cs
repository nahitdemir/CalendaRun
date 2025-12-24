using Catalog.Application.Common;

namespace Catalog.Application.Events.Queries;

public record GetEventByIdQuery(Guid EventId, Guid? TenantId) : IQuery<Result<EventDto>>;

