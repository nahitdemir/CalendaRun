namespace Notifications.Domain;

public class NotificationLog
{
    public Guid Id { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public string Type { get; set; } = string.Empty;
    public string DataJson { get; set; } = string.Empty;
}

