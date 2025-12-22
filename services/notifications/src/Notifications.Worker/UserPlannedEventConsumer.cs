using System.Text.Json;
using Calendarun.Contracts.Planning;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Notifications.Domain;
using Notifications.Infrastructure;
using SerilogContext = Serilog.Context.LogContext;

namespace Notifications.Worker;

public class UserPlannedEventConsumer : IConsumer<PlanningUserPlannedV1>
{
    private readonly NotificationsDbContext _db;
    private readonly EmailService _emailService;
    private readonly ILogger<UserPlannedEventConsumer> _logger;

    public UserPlannedEventConsumer(
        NotificationsDbContext db,
        EmailService emailService,
        ILogger<UserPlannedEventConsumer> logger)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PlanningUserPlannedV1> context)
    {
        var message = context.Message;
        var messageId = context.MessageId?.ToString() ?? message.PlanItemId.ToString();
        
        using (SerilogContext.PushProperty("PlanItemId", message.PlanItemId))
        using (SerilogContext.PushProperty("CorrelationId", messageId))
        {
        
        _logger.LogInformation("✅ Consumed PlanningUserPlannedV1: {@Message}, MessageId={MessageId}", message, messageId);

        // Idempotency check
        var existing = await _db.NotificationLogs
            .FirstOrDefaultAsync(l => l.MessageId == messageId, context.CancellationToken);
        
        if (existing != null)
        {
            _logger.LogInformation("⚠️ Message already processed, skipping. MessageId={MessageId}", messageId);
            return;
        }

        // Log to database
        var log = new NotificationLog
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            OccurredAt = DateTimeOffset.UtcNow,
            Type = "UserPlanned",
            DataJson = JsonSerializer.Serialize(message)
        };

        _db.NotificationLogs.Add(log);
        await _db.SaveChangesAsync(context.CancellationToken);

        // Send email
        try
        {
            var to = message.UserEmail;
            var subject = "Planned event";
            var body = $"EventId={message.EventId} PlanItemId={message.PlanItemId}";

            _logger.LogInformation("✉️ Sending email to {To} via SMTP...", to);

            await _emailService.SendEmailAsync(to, subject, body, context.CancellationToken);

            _logger.LogInformation("✅ Email sent OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Email send failed");
            throw; // kritik: hata gizlenmesin
        }
        
        } // End LogContext
    }
}

