namespace Calendarun.Contracts.Settings;

/// <summary>
/// Event: Settings have been changed
/// Exchange: settings.changed.v1
/// Publisher: Settings.Api
/// Consumers: All services using SettingsClient
/// </summary>
/// <remarks>
/// Schema Version: 1.0
/// Used for cache invalidation across services
/// </remarks>
public record SettingsChangedV1(
    /// <summary>Tenant identifier (null = global)</summary>
    string? TenantId,
    
    /// <summary>Changed setting keys</summary>
    string[] Keys,
    
    /// <summary>New version number</summary>
    long Version,
    
    /// <summary>When the change occurred</summary>
    DateTimeOffset OccurredAt
)
{
    /// <summary>Exchange name for MassTransit routing</summary>
    public const string ExchangeName = "settings.changed.v1";
}

