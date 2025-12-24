namespace Planning.Domain;

public class UserPlanItem
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid EventId { get; set; }
    public string State { get; set; } = "Active";
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    
    public User User { get; set; } = null!;
}
