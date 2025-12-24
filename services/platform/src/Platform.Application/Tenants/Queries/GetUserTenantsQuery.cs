using Platform.Application.Common;

namespace Platform.Application.Tenants.Queries;

public record GetUserTenantsQuery(Guid UserId) : IQuery<Result<List<UserTenantDto>>>;

public record UserTenantDto(
    Guid TenantId,
    string TenantName,
    string TenantSlug,
    string Role,
    DateTimeOffset CreatedAt
);

