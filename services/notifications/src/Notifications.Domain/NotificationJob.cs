namespace Notifications.Domain;

public class NotificationJob
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public Guid UserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public DateTimeOffset ScheduledAt { get; set; }
    public NotificationJobStatus Status { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? RecipientAddress { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? LastError { get; set; }

    public ICollection<DeliveryAttempt> DeliveryAttempts { get; set; } = new List<DeliveryAttempt>();
}

public enum NotificationChannel
{
    Email,
    Sms,
    Push
}

public enum NotificationJobStatus
{
    Pending,
    Processing,
    Sent,
    Failed,
    Cancelled
}
