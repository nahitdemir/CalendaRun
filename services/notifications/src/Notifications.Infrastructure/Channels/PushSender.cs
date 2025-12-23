using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Notifications.Domain;

namespace Notifications.Infrastructure.Channels;

/// <summary>
/// Stub Push notification sender - implement actual provider (Firebase, APNS, etc.) when needed
/// </summary>
public class PushSender : IChannelSender
{
    private readonly ILogger<PushSender> _logger;

    public PushSender(ILogger<PushSender> logger)
    {
        _logger = logger;
    }

    public NotificationChannel Channel => NotificationChannel.Push;
    public string ProviderName => "Stub-Push";

    public Task<ChannelSendResult> SendAsync(NotificationJob job, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        
        // Stub implementation - log and return success
        _logger.LogInformation("🔔 [STUB] Push notification would be sent to user {UserId}: {Subject}", 
            job.UserId, 
            job.Subject);

        sw.Stop();
        return Task.FromResult(new ChannelSendResult(true, null, (int)sw.ElapsedMilliseconds));
    }
}

