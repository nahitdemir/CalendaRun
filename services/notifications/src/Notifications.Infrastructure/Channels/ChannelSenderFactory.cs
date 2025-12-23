using Microsoft.Extensions.DependencyInjection;
using Notifications.Domain;

namespace Notifications.Infrastructure.Channels;

public interface IChannelSenderFactory
{
    IChannelSender GetSender(NotificationChannel channel);
}

public class ChannelSenderFactory : IChannelSenderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<NotificationChannel, Type> _senderTypes;

    public ChannelSenderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _senderTypes = new Dictionary<NotificationChannel, Type>
        {
            { NotificationChannel.Email, typeof(EmailSender) },
            { NotificationChannel.Sms, typeof(SmsSender) },
            { NotificationChannel.Push, typeof(PushSender) }
        };
    }

    public IChannelSender GetSender(NotificationChannel channel)
    {
        if (!_senderTypes.TryGetValue(channel, out var senderType))
        {
            throw new NotSupportedException($"Channel {channel} is not supported");
        }

        return (IChannelSender)_serviceProvider.GetRequiredService(senderType);
    }
}

