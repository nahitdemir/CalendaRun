namespace Settings.Domain;

public class SettingDefinition
{
    public string Key { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SettingValueType ValueType { get; set; }
    public string? JsonSchema { get; set; }
    public bool IsRequired { get; set; }
    public bool IsSensitive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public enum SettingValueType
{
    String,
    Integer,
    Decimal,
    Boolean,
    Json,
    StringArray
}

