using Catalog.Application.Common;

namespace Catalog.Application.Events.Commands;

public record DeleteEventCommand(
    Guid EventId,
    Guid? TenantId,
    Guid UserId,
    string? UserEmail,
    bool IsSuperAdmin,
    string? TraceId
) : ICommand<Result>;

