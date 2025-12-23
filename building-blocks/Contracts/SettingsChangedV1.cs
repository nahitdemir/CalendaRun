namespace Calendarun.Contracts.Settings;

public record SettingsChangedV1(
    string? TenantId,
    string[] Keys,
    long Version,
    DateTimeOffset OccurredAt
);

