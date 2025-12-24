namespace Platform.Domain.Entities;

public class Membership
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public TenantRole Role { get; set; }
    public MembershipStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? InvitedBy { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
}

public enum TenantRole
{
    TenantAdmin,
    TenantUser
}

public enum MembershipStatus
{
    Active,
    Suspended,
    Removed
}

