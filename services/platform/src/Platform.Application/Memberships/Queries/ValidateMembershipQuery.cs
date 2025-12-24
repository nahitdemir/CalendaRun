using Platform.Application.Common;

namespace Platform.Application.Memberships.Queries;

public record ValidateMembershipQuery(
    Guid UserId,
    Guid TenantId
) : IQuery<MembershipValidationResult>;

public record MembershipValidationResult(bool IsMember, string? Role);

