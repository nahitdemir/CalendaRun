using System.Text.Json;
using Calendarun.Contracts.Planning;
using Calendarun.Settings.Client;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Notifications.Domain;
using Notifications.Infrastructure;
using Notifications.Infrastructure.Templates;
using SerilogContext = Serilog.Context.LogContext;

namespace Notifications.Worker.Consumers;

public class UserPlannedEventConsumer : IConsumer<PlanningUserPlannedV1>
{
    private readonly NotificationsDbContext _db;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly ISettingsClient _settingsClient;
    private readonly ILogger<UserPlannedEventConsumer> _logger;

    public UserPlannedEventConsumer(
        NotificationsDbContext db,
        ITemplateRenderer templateRenderer,
        ISettingsClient settingsClient,
        ILogger<UserPlannedEventConsumer> logger)
    {
        _db = db;
        _templateRenderer = templateRenderer;
        _settingsClient = settingsClient;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PlanningUserPlannedV1> context)
    {
        var message = context.Message;
        var messageId = context.MessageId?.ToString() ?? message.PlanItemId.ToString();
        
        using (SerilogContext.PushProperty("PlanItemId", message.PlanItemId))
        using (SerilogContext.PushProperty("CorrelationId", messageId))
        using (SerilogContext.PushProperty("TenantId", message.TenantId))
        {
            _logger.LogInformation("📥 Received PlanningUserPlannedV1: {@Message}, MessageId={MessageId}", message, messageId);

            // Inbox pattern: Idempotency check
            var alreadyProcessed = await _db.ProcessedMessages
                .AnyAsync(p => p.MessageId == messageId, context.CancellationToken);
            
            if (alreadyProcessed)
            {
                _logger.LogInformation("⚠️ Message already processed, skipping. MessageId={MessageId}", messageId);
                return;
            }

            var payloadJson = JsonSerializer.Serialize(message);
            var tenantIdStr = message.TenantId?.ToString();
            
            // Create immediate email job
            var immediateJobKey = $"userplanned:email:{message.PlanItemId}";
            await CreateJobIfNotExistsAsync(
                userId: message.UserId,
                eventType: "PlanningUserPlannedV1",
                payloadJson: payloadJson,
                channel: NotificationChannel.Email,
                scheduledAt: DateTimeOffset.UtcNow,
                idempotencyKey: immediateJobKey,
                recipientEmail: message.UserEmail,
                tenantId: message.TenantId,
                tenantIdStr: tenantIdStr,
                context.CancellationToken);

            // Create reminder jobs based on settings
            var reminderOffsets = await _settingsClient.GetAsync<int[]>("notifications.reminder.offsets_minutes", tenantIdStr, context.CancellationToken);
            if (reminderOffsets != null && reminderOffsets.Length > 0)
            {
                // For now, schedule reminders based on current time + offset
                // In production, you'd get event start time from Catalog service
                foreach (var offsetMinutes in reminderOffsets)
                {
                    var reminderKey = $"userplanned:reminder:{message.PlanItemId}:{offsetMinutes}";
                    var scheduledAt = DateTimeOffset.UtcNow.AddMinutes(offsetMinutes);
                    
                    await CreateJobIfNotExistsAsync(
                        userId: message.UserId,
                        eventType: "ReminderNotification",
                        payloadJson: payloadJson,
                        channel: NotificationChannel.Email,
                        scheduledAt: scheduledAt,
                        idempotencyKey: reminderKey,
                        recipientEmail: message.UserEmail,
                        tenantId: message.TenantId,
                        tenantIdStr: tenantIdStr,
                        context.CancellationToken);
                    
                    _logger.LogInformation("📅 Scheduled reminder job: {Key} at {ScheduledAt}", reminderKey, scheduledAt);
                }
            }

            // Mark message as processed
            _db.ProcessedMessages.Add(new ProcessedMessage
            {
                MessageId = messageId,
                EventType = "PlanningUserPlannedV1",
                ProcessedAt = DateTimeOffset.UtcNow
            });

            await _db.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation("✅ Event consumed, jobs created. MessageId={MessageId}", messageId);
        }
    }

    private async Task CreateJobIfNotExistsAsync(
        Guid userId,
        string eventType,
        string payloadJson,
        NotificationChannel channel,
        DateTimeOffset scheduledAt,
        string idempotencyKey,
        string recipientEmail,
        Guid? tenantId,
        string? tenantIdStr,
        CancellationToken ct)
    {
        // Check if job already exists
        var exists = await _db.NotificationJobs
            .AnyAsync(j => j.IdempotencyKey == idempotencyKey, ct);
        
        if (exists)
        {
            _logger.LogDebug("Job already exists: {IdempotencyKey}", idempotencyKey);
            return;
        }

        // Render template
        var (subject, body) = await _templateRenderer.RenderAsync(eventType, payloadJson, tenantIdStr, ct);

        var job = new NotificationJob
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            EventType = eventType,
            PayloadJson = payloadJson,
            Channel = channel,
            ScheduledAt = scheduledAt,
            Status = NotificationJobStatus.Pending,
            IdempotencyKey = idempotencyKey,
            RecipientAddress = recipientEmail,
            Subject = subject,
            Body = body,
            AttemptCount = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.NotificationJobs.Add(job);
        _logger.LogInformation("📝 Created notification job: {JobId} Channel={Channel} ScheduledAt={ScheduledAt}", 
            job.Id, channel, scheduledAt);
    }
}
