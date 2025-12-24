using Calendarun.Settings.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notifications.Domain;
using Notifications.Infrastructure;
using Notifications.Infrastructure.Channels;

namespace Notifications.Worker.Dispatcher;

public class NotificationDispatcher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationDispatcher> _logger;
    
    // Metrics
    private static int _jobsProcessed;
    private static int _jobsFailed;
    private static DateTimeOffset? _lastSuccessfulRun;

    public NotificationDispatcher(
        IServiceProvider serviceProvider,
        ILogger<NotificationDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public static (int processed, int failed, DateTimeOffset? lastRun) GetStats() => 
        (_jobsProcessed, _jobsFailed, _lastSuccessfulRun);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("📮 NotificationDispatcher started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueJobsAsync(stoppingToken);
                _lastSuccessfulRun = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ NotificationDispatcher error");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        _logger.LogInformation("📮 NotificationDispatcher stopped");
    }

    private async Task ProcessDueJobsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var channelFactory = scope.ServiceProvider.GetRequiredService<IChannelSenderFactory>();
        var settingsClient = scope.ServiceProvider.GetRequiredService<ISettingsClient>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<NotificationDispatcher>>();

        // Get settings
        var maxAttempts = await settingsClient.GetAsync<int?>("notifications.dispatcher.max_attempts", null, ct) ?? 3;
        var batchSize = await settingsClient.GetAsync<int?>("notifications.dispatcher.batch_size", null, ct) ?? 50;
        var retryBackoffBaseSeconds = await settingsClient.GetAsync<int?>("notifications.dispatcher.retry_backoff_base_seconds", null, ct) ?? 10;

        // Select due jobs
        var now = DateTimeOffset.UtcNow;
        var dueJobs = await db.NotificationJobs
            .Where(j => j.Status == NotificationJobStatus.Pending && j.ScheduledAt <= now)
            .OrderBy(j => j.ScheduledAt)
            .Take(batchSize)
            .ToListAsync(ct);

        if (dueJobs.Count == 0)
            return;

        logger.LogInformation("📤 Processing {Count} due notification jobs", dueJobs.Count);

        foreach (var job in dueJobs)
        {
            await ProcessJobAsync(db, channelFactory, job, maxAttempts, retryBackoffBaseSeconds, ct);
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task ProcessJobAsync(
        NotificationsDbContext db,
        IChannelSenderFactory channelFactory,
        NotificationJob job,
        int maxAttempts,
        int retryBackoffBaseSeconds,
        CancellationToken ct)
    {
        try
        {
            job.Status = NotificationJobStatus.Processing;
            job.AttemptCount++;

            _logger.LogInformation("🔄 Processing job {JobId} Channel={Channel} Attempt={Attempt}", 
                job.Id, job.Channel, job.AttemptCount);

            var sender = channelFactory.GetSender(job.Channel);
            var result = await sender.SendAsync(job, ct);

            // Record attempt
            var attempt = new DeliveryAttempt
            {
                Id = Guid.NewGuid(),
                JobId = job.Id,
                AttemptNo = job.AttemptCount,
                Provider = sender.ProviderName,
                Status = result.Success ? DeliveryStatus.Success : DeliveryStatus.Failed,
                Error = result.Error,
                DurationMs = result.DurationMs,
                OccurredAt = DateTimeOffset.UtcNow
            };
            db.DeliveryAttempts.Add(attempt);

            if (result.Success)
            {
                job.Status = NotificationJobStatus.Sent;
                job.ProcessedAt = DateTimeOffset.UtcNow;
                job.LastError = null;
                Interlocked.Increment(ref _jobsProcessed);

                _logger.LogInformation("✅ Job {JobId} sent successfully in {Duration}ms", job.Id, result.DurationMs);
            }
            else
            {
                job.LastError = result.Error;

                if (job.AttemptCount >= maxAttempts)
                {
                    job.Status = NotificationJobStatus.Failed;
                    job.ProcessedAt = DateTimeOffset.UtcNow;
                    Interlocked.Increment(ref _jobsFailed);

                    _logger.LogWarning("❌ Job {JobId} failed after {Attempts} attempts: {Error}", 
                        job.Id, job.AttemptCount, result.Error);
                }
                else
                {
                    // Schedule retry with exponential backoff
                    var backoffSeconds = Math.Pow(2, job.AttemptCount) * retryBackoffBaseSeconds; // 20s, 40s, 80s, etc. (if base=10)
                    job.ScheduledAt = DateTimeOffset.UtcNow.AddSeconds(backoffSeconds);
                    job.Status = NotificationJobStatus.Pending;

                    _logger.LogInformation("🔄 Job {JobId} will retry in {Seconds}s (attempt {Attempt}/{Max})", 
                        job.Id, backoffSeconds, job.AttemptCount, maxAttempts);
                }
            }
        }
        catch (Exception ex)
        {
            job.LastError = ex.Message;
            job.Status = NotificationJobStatus.Pending;

            _logger.LogError(ex, "❌ Exception processing job {JobId}", job.Id);
        }
    }
}

