namespace Planning.Domain;

public class UserPlanItem
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; } // Nullable - plan can be for public events
    public Guid UserId { get; set; }
    public Guid EventId { get; set; }
    public PlanState State { get; set; } = PlanState.Active;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    
    public User User { get; set; } = null!;
}
