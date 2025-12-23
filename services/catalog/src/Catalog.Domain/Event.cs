namespace Catalog.Domain;

public class Event
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public string City { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string RegistrationUrl { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
