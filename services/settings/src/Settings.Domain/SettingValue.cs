namespace Settings.Domain;

public class SettingValue
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? TenantId { get; set; }
    public string Environment { get; set; } = "dev";
    public string ValueJson { get; set; } = string.Empty;
    public long Version { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    
    public SettingDefinition Definition { get; set; } = null!;
}

