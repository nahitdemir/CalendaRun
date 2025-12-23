namespace Settings.Domain;

public class SettingAudit
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? TenantId { get; set; }
    public string? OldValueJson { get; set; }
    public string NewValueJson { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}

