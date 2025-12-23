using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Notifications.Domain;

namespace Notifications.Infrastructure.Channels;

/// <summary>
/// Stub SMS sender - implement actual provider (Twilio, etc.) when needed
/// </summary>
public class SmsSender : IChannelSender
{
    private readonly ILogger<SmsSender> _logger;

    public SmsSender(ILogger<SmsSender> logger)
    {
        _logger = logger;
    }

    public NotificationChannel Channel => NotificationChannel.Sms;
    public string ProviderName => "Stub-SMS";

    public Task<ChannelSendResult> SendAsync(NotificationJob job, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        
        // Stub implementation - log and return success
        _logger.LogInformation("📱 [STUB] SMS would be sent to {Recipient}: {Body}", 
            job.RecipientAddress, 
            job.Body?.Length > 50 ? job.Body[..50] + "..." : job.Body);

        sw.Stop();
        return Task.FromResult(new ChannelSendResult(true, null, (int)sw.ElapsedMilliseconds));
    }
}

