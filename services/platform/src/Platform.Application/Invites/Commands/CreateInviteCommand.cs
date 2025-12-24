using Platform.Application.Common;
using Platform.Domain.Entities;

namespace Platform.Application.Invites.Commands;

public record CreateInviteCommand(
    Guid TenantId,
    string Email,
    TenantRole Role,
    Guid ActorUserId,
    string? ActorEmail
) : ICommand<Result<CreateInviteResult>>;

public record CreateInviteResult(
    Guid Id,
    string Email,
    string Role,
    string Token,
    DateTimeOffset ExpiresAt
);

