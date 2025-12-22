using System.Text.Json;
using Calendarun.Contracts.Planning;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Planning.Infrastructure;

namespace Planning.Api;

public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(IServiceProvider serviceProvider, ILogger<OutboxProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("📮 OutboxProcessor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing outbox messages");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        _logger.LogInformation("📮 OutboxProcessor stopped");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlanningDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var pendingMessages = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(10)
            .ToListAsync(ct);

        if (pendingMessages.Count == 0)
            return;

        _logger.LogInformation("📤 Processing {Count} outbox messages", pendingMessages.Count);

        foreach (var message in pendingMessages)
        {
            try
            {
                if (message.Type == nameof(PlanningUserPlannedV1))
                {
                    var eventMessage = JsonSerializer.Deserialize<PlanningUserPlannedV1>(message.Payload);
                    if (eventMessage != null)
                    {
                        _logger.LogInformation("📣 Publishing PlanningUserPlannedV1 from outbox... MessageId={MessageId}", message.Id);
                        
                        await publishEndpoint.Publish(eventMessage, ct);
                        
                        message.ProcessedAt = DateTimeOffset.UtcNow;
                        message.Error = null;
                        
                        _logger.LogInformation("✅ Published from outbox. MessageId={MessageId}", message.Id);
                    }
                }

                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to publish outbox message. MessageId={MessageId}", message.Id);
                
                message.RetryCount++;
                message.Error = ex.Message;
                await db.SaveChangesAsync(ct);
            }
        }

        // Record successful run for health check
        OutboxHealthCheck.RecordSuccessfulRun(pendingMessages.Count);
    }
}

