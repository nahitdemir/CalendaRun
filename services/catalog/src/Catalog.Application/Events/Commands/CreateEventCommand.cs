using Catalog.Application.Common;

namespace Catalog.Application.Events.Commands;

public record CreateEventCommand(
    Guid TenantId,
    Guid UserId,
    string? UserEmail,
    string Title,
    string? Description,
    DateTimeOffset StartAt,
    string City,
    string CountryCode,
    string? RegistrationUrl,
    bool IsGlobal,
    bool IsSuperAdmin,
    string? TraceId
) : ICommand<Result<CreateEventResult>>;

public record CreateEventResult(
    Guid Id,
    Guid? TenantId,
    string Title,
    DateTimeOffset StartAt
);

