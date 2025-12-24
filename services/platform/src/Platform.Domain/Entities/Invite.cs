namespace Platform.Domain.Entities;

public class Invite
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public TenantRole Role { get; set; }
    public string Token { get; set; } = string.Empty;
    public InviteStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
}

public enum InviteStatus
{
    Pending,
    Accepted,
    Expired,
    Revoked
}

