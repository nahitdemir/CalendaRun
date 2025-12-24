using Catalog.Application.Common;

namespace Catalog.Application.Events.Queries;

public record GetAdminEventsQuery(
    Guid? TenantId,
    bool IsSuperAdmin
) : IQuery<Result<List<AdminEventDto>>>;

public record AdminEventDto(
    Guid Id,
    Guid? TenantId,
    string Title,
    string? Description,
    DateTimeOffset StartAt,
    string City,
    string CountryCode,
    string? RegistrationUrl,
    DateTimeOffset CreatedAt,
    Guid? CreatedBy,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedBy,
    bool IsGlobal
);

