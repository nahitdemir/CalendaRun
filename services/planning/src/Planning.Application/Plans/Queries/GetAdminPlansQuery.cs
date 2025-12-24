using Planning.Application.Common;

namespace Planning.Application.Plans.Queries;

public record GetAdminPlansQuery(
    Guid? TenantId,
    bool IsSuperAdmin
) : IQuery<Result<List<AdminPlanDto>>>;

public record AdminPlanDto(
    Guid Id,
    Guid TenantId,
    Guid UserId,
    string UserEmail,
    Guid EventId,
    string State,
    DateTimeOffset CreatedAt,
    Guid? CreatedBy
);

