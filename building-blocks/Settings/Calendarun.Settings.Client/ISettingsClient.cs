namespace Calendarun.Settings.Client;

public interface ISettingsClient
{
    Task<T?> GetAsync<T>(string key, string? tenantId = null, CancellationToken ct = default);
    Task<IDictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, string? tenantId = null, CancellationToken ct = default);
    Task InvalidateAsync(string key, string? tenantId = null, CancellationToken ct = default);
    Task InvalidateAllAsync(string? tenantId = null, CancellationToken ct = default);
}

