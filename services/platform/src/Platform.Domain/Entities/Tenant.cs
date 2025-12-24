using Calendarun.Common;

namespace Platform.Domain.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string DefaultLanguage { get; set; } = Defaults.Language;
    public string DefaultCurrency { get; set; } = Defaults.Currency;
    public TenantStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    public ICollection<Invite> Invites { get; set; } = new List<Invite>();
}

public enum TenantStatus
{
    Active,
    Suspended,
    Deleted
}
