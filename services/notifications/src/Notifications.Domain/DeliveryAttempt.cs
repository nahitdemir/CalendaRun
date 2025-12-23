namespace Notifications.Domain;

public class DeliveryAttempt
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public int AttemptNo { get; set; }
    public string Provider { get; set; } = string.Empty;
    public DeliveryStatus Status { get; set; }
    public string? Error { get; set; }
    public int DurationMs { get; set; }
    public DateTimeOffset OccurredAt { get; set; }

    public NotificationJob Job { get; set; } = null!;
}

public enum DeliveryStatus
{
    Success,
    Failed,
    Timeout
}

