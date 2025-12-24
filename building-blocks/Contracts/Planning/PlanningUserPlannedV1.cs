namespace Calendarun.Contracts.Planning;

/// <summary>
/// Event: User has planned to attend an event
/// Exchange: planning.userplanned.v1
/// Publisher: Planning.Api
/// Consumers: Notifications.Worker
/// </summary>
/// <remarks>
/// Schema Version: 1.1 - Added TenantId for multi-tenant support
/// Breaking changes require new version (v2)
/// </remarks>
public record PlanningUserPlannedV1(
    /// <summary>User's unique identifier</summary>
    Guid UserId,
    
    /// <summary>User's email for notifications</summary>
    string UserEmail,
    
    /// <summary>Catalog event identifier</summary>
    Guid EventId,
    
    /// <summary>Plan item identifier (unique)</summary>
    Guid PlanItemId,
    
    /// <summary>User's timezone (IANA format, e.g. "Europe/Istanbul")</summary>
    string Timezone,
    
    /// <summary>When the plan was created</summary>
    DateTimeOffset OccurredAt,
    
    /// <summary>Tenant identifier for multi-tenant isolation</summary>
    Guid? TenantId = null
)
{
    /// <summary>Exchange name for MassTransit routing</summary>
    public const string ExchangeName = "planning.userplanned.v1";
}
