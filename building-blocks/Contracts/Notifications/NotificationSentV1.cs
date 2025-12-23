namespace Calendarun.Contracts.Notifications;

/// <summary>
/// Event: Notification has been sent successfully
/// Exchange: notifications.sent.v1
/// Publisher: Notifications.Worker
/// Consumers: (future) Analytics, Audit
/// </summary>
/// <remarks>
/// Schema Version: 1.0
/// </remarks>
public record NotificationSentV1(
    /// <summary>Notification job identifier</summary>
    Guid JobId,
    
    /// <summary>User identifier</summary>
    Guid UserId,
    
    /// <summary>Channel used (Email, Sms, Push)</summary>
    string Channel,
    
    /// <summary>Recipient address</summary>
    string RecipientAddress,
    
    /// <summary>When the notification was sent</summary>
    DateTimeOffset SentAt,
    
    /// <summary>Duration in milliseconds</summary>
    int DurationMs
)
{
    /// <summary>Exchange name for MassTransit routing</summary>
    public const string ExchangeName = "notifications.sent.v1";
}

