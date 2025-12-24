namespace Planning.Domain;

public class User
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; } // Nullable - user can exist without tenant
    public string Email { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
