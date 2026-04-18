namespace NotificationHub;

public interface IChannelRouter
{
    IChannelSender Resolve(Channel channel);
}
