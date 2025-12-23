using Notifications.Domain;

namespace Notifications.Infrastructure.Channels;

public interface IChannelSender
{
    NotificationChannel Channel { get; }
    string ProviderName { get; }
    Task<ChannelSendResult> SendAsync(NotificationJob job, CancellationToken ct = default);
}

public record ChannelSendResult(
    bool Success,
    string? Error = null,
    int DurationMs = 0
);

