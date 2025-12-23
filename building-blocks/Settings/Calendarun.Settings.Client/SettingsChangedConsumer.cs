using Calendarun.Contracts.Settings;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Calendarun.Settings.Client;

public class SettingsChangedConsumer : IConsumer<SettingsChangedV1>
{
    private readonly ISettingsClient _settingsClient;
    private readonly ILogger<SettingsChangedConsumer> _logger;
    private readonly SettingsClientOptions _options;

    public SettingsChangedConsumer(
        ISettingsClient settingsClient,
        ILogger<SettingsChangedConsumer> logger,
        SettingsClientOptions options)
    {
        _settingsClient = settingsClient;
        _logger = logger;
        _options = options;
    }

    public async Task Consume(ConsumeContext<SettingsChangedV1> context)
    {
        var message = context.Message;
        
        _logger.LogInformation(
            "🔔 Settings changed: TenantId={TenantId}, Keys=[{Keys}], Version={Version}",
            message.TenantId ?? "global",
            string.Join(", ", message.Keys),
            message.Version);

        // Invalidate L1 cache for changed keys
        foreach (var key in message.Keys)
        {
            await _settingsClient.InvalidateAsync(key, message.TenantId, context.CancellationToken);
        }

        // Optionally refresh if keys are in warmup list
        var keysToRefresh = message.Keys.Intersect(_options.WarmupKeys).ToList();
        if (keysToRefresh.Count > 0)
        {
            _logger.LogInformation("♻️ Refreshing {Count} warmup keys", keysToRefresh.Count);
            await _settingsClient.GetManyAsync<object>(keysToRefresh, message.TenantId, context.CancellationToken);
        }

        _logger.LogInformation("✅ Settings invalidation complete");
    }
}

