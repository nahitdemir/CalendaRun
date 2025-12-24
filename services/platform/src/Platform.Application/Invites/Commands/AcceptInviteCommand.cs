using Platform.Application.Common;

namespace Platform.Application.Invites.Commands;

public record AcceptInviteCommand(
    string Token,
    Guid UserId,
    string UserEmail
) : ICommand<Result<AcceptInviteResult>>;

public record AcceptInviteResult(
    Guid TenantId,
    string TenantName,
    string Role
);

