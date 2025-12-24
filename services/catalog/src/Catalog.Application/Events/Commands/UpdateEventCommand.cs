using Catalog.Application.Common;

namespace Catalog.Application.Events.Commands;

public record UpdateEventCommand(
    Guid EventId,
    Guid? TenantId,
    Guid UserId,
    string? UserEmail,
    bool IsSuperAdmin,
    string? Title,
    string? Description,
    DateTimeOffset? StartAt,
    string? City,
    string? CountryCode,
    string? RegistrationUrl,
    string? TraceId
) : ICommand<Result<UpdateEventResult>>;

public record UpdateEventResult(
    Guid Id,
    string Title,
    DateTimeOffset? UpdatedAt
);

