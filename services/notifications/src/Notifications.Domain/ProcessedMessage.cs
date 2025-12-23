namespace Notifications.Domain;

/// <summary>
/// Inbox pattern - idempotency için consume edilen mesajları takip eder
/// </summary>
public class ProcessedMessage
{
    public string MessageId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; set; }
}

