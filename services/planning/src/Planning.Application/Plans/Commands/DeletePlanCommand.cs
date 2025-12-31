using Planning.Application.Common;

namespace Planning.Application.Plans.Commands;

public record DeletePlanCommand(
    Guid PlanId,
    Guid? TenantId,
    Guid UserId,
    string? UserEmail,
    bool IsSuperAdmin,
    bool IsTenantAdmin,
    string? TraceId
) : ICommand<Result>;
