using Platform.Application.Common;

namespace Platform.Application.Tenants.Queries;

public record GetAllTenantsQuery() : IQuery<Result<List<TenantDto>>>;

public record TenantDto(
    Guid Id,
    string Name,
    string Slug,
    string DefaultLanguage,
    string DefaultCurrency,
    string Status,
    DateTimeOffset CreatedAt,
    int MemberCount
);

