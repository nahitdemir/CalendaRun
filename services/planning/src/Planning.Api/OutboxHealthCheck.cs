using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Planning.Infrastructure;

namespace Planning.Api;

public class OutboxHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    private static DateTimeOffset? _lastSuccessfulRun;
    private static int _pendingCount;

    public OutboxHealthCheck(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public static void RecordSuccessfulRun(int pendingCount)
    {
        _lastSuccessfulRun = DateTimeOffset.UtcNow;
        _pendingCount = pendingCount;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PlanningDbContext>();

            var pending = await db.OutboxMessages
                .Where(m => m.ProcessedAt == null)
                .CountAsync(cancellationToken);

            var data = new Dictionary<string, object>
            {
                { "pending_messages", pending },
                { "last_successful_run", _lastSuccessfulRun?.ToString("O") ?? "never" },
                { "seconds_since_last_run", _lastSuccessfulRun.HasValue 
                    ? (int)(DateTimeOffset.UtcNow - _lastSuccessfulRun.Value).TotalSeconds 
                    : -1 }
            };

            // Unhealthy if: no run in last 60 seconds OR too many pending (>100)
            if (!_lastSuccessfulRun.HasValue || 
                (DateTimeOffset.UtcNow - _lastSuccessfulRun.Value).TotalSeconds > 60)
            {
                return HealthCheckResult.Unhealthy(
                    "Outbox processor not running or stalled", 
                    data: data);
            }

            if (pending > 100)
            {
                return HealthCheckResult.Degraded(
                    $"Too many pending outbox messages: {pending}", 
                    data: data);
            }

            return HealthCheckResult.Healthy($"Outbox healthy, {pending} pending", data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Outbox health check failed", ex);
        }
    }
}

