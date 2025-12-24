using Platform.Application.Common;

namespace Platform.Application.Memberships.Queries;

public record GetTenantUsersQuery(Guid TenantId) : IQuery<Result<List<TenantUserDto>>>;

public record TenantUserDto(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string Role,
    DateTimeOffset CreatedAt,
    DateTimeOffset? AcceptedAt
);

