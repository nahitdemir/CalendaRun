using Planning.Application.Common;

namespace Planning.Application.Plans.Commands;

public record CreatePlanCommand(
    Guid? TenantId, // Optional - null for public events
    Guid UserId,
    string UserEmail,
    Guid EventId,
    string? TraceId
) : ICommand<Result<CreatePlanResult>>;

public record CreatePlanResult(
    Guid PlanItemId,
    Guid? TenantId,
    Guid UserId,
    Guid EventId,
    string State,
    DateTimeOffset CreatedAt,
    string Timezone
);

