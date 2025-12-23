namespace Calendarun.Contracts.Notifications;

/// <summary>
/// Event: Notification delivery has failed
/// Exchange: notifications.failed.v1
/// Publisher: Notifications.Worker
/// Consumers: (future) Alerting, Retry handler
/// </summary>
/// <remarks>
/// Schema Version: 1.0
/// </remarks>
public record NotificationFailedV1(
    /// <summary>Notification job identifier</summary>
    Guid JobId,
    
    /// <summary>User identifier</summary>
    Guid UserId,
    
    /// <summary>Channel attempted (Email, Sms, Push)</summary>
    string Channel,
    
    /// <summary>Recipient address</summary>
    string RecipientAddress,
    
    /// <summary>Error message</summary>
    string Error,
    
    /// <summary>Number of attempts made</summary>
    int AttemptCount,
    
    /// <summary>When the failure occurred</summary>
    DateTimeOffset FailedAt
)
{
    /// <summary>Exchange name for MassTransit routing</summary>
    public const string ExchangeName = "notifications.failed.v1";
}

