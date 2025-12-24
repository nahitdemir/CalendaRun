using Planning.Application.Common;

namespace Planning.Application.Users.Queries;

public record GetAdminUsersQuery(
    Guid? TenantId,
    bool IsSuperAdmin
) : IQuery<Result<List<AdminUserDto>>>;

public record AdminUserDto(
    Guid Id,
    Guid? TenantId,
    string Email,
    DateTimeOffset CreatedAt,
    int PlanCount
);

