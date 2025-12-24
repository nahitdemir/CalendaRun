namespace Catalog.Domain;

public class Event
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; } // null = global event
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public string City { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string RegistrationUrl { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
