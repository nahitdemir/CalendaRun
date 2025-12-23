using Calendarun.Contracts.Settings;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Calendarun.Settings.Client;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSettingsClient(
        this IServiceCollection services,
        Action<SettingsClientOptions>? configure = null)
    {
        var options = new SettingsClientOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddMemoryCache();
        
        // Add Redis connection
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(options.RedisConnectionString));

        // Add HTTP client
        services.AddHttpClient<ISettingsClient, SettingsClient>((sp, client) =>
        {
            client.BaseAddress = new Uri(options.SettingsServiceUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // Register warmup service
        if (options.WarmupKeys.Length > 0)
        {
            services.AddHostedService<SettingsWarmupService>();
        }

        return services;
    }

    public static void AddSettingsChangedConsumer(
        this IBusRegistrationConfigurator configurator,
        string serviceName)
    {
        configurator.AddConsumer<SettingsChangedConsumer>();
    }

    public static void ConfigureSettingsChangedEndpoint(
        this IRabbitMqBusFactoryConfigurator cfg,
        IBusRegistrationContext context,
        string serviceName)
    {
        cfg.Message<SettingsChangedV1>(m => m.SetEntityName("settings.changed.v1"));
        
        cfg.ReceiveEndpoint($"settings.changed.v1.{serviceName}", e =>
        {
            e.ConfigureConsumer<SettingsChangedConsumer>(context);
        });
    }
}

public class SettingsWarmupService : IHostedService
{
    private readonly ISettingsClient _settingsClient;
    private readonly SettingsClientOptions _options;
    private readonly ILogger<SettingsWarmupService> _logger;

    public SettingsWarmupService(
        ISettingsClient settingsClient,
        SettingsClientOptions options,
        ILogger<SettingsWarmupService> logger)
    {
        _settingsClient = settingsClient;
        _options = options;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔥 Warming up settings cache with {Count} keys...", _options.WarmupKeys.Length);
        
        try
        {
            var settings = await _settingsClient.GetManyAsync<object>(_options.WarmupKeys, null, cancellationToken);
            _logger.LogInformation("✅ Settings warmup complete: {Loaded}/{Total} keys loaded",
                settings.Count(kv => kv.Value != null),
                _options.WarmupKeys.Length);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Settings warmup failed, will load on demand");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

