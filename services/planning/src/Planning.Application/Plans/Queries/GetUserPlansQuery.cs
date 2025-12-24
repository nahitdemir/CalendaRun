using Planning.Application.Common;

namespace Planning.Application.Plans.Queries;

public record GetUserPlansQuery(
    Guid TenantId,
    Guid UserId
) : IQuery<Result<List<PlanDto>>>;

public record PlanDto(
    Guid Id,
    Guid EventId,
    string State,
    DateTimeOffset CreatedAt
);

