using Planning.Application.Common;
using Planning.Domain;

namespace Planning.Application.Plans.Queries;

public record GetUserPlansQuery(
    Guid? TenantId, // Optional filter - null returns all user's plans
    Guid UserId
) : IQuery<Result<List<PlanDto>>>;

public record PlanDto(
    Guid Id,
    Guid EventId,
    PlanState State,
    DateTimeOffset CreatedAt
);

