using Platform.Application.Common;

namespace Platform.Application.Tenants.Commands;

public record CreateTenantCommand(
    string Name,
    string? Slug,
    string? Description,
    string? DefaultLanguage,
    string? DefaultCurrency,
    Guid ActorUserId,
    string? ActorEmail
) : ICommand<Result<CreateTenantResult>>;

public record CreateTenantResult(
    Guid Id,
    string Name,
    string Slug,
    string DefaultLanguage,
    string DefaultCurrency,
    DateTimeOffset CreatedAt
);

