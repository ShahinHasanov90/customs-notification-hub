namespace NotificationHub;

public sealed class ChannelRouter : IChannelRouter
{
    private readonly IReadOnlyDictionary<Channel, IChannelSender> _byChannel;

    public ChannelRouter(IEnumerable<IChannelSender> senders)
    {
        // Later registrations win; this makes it trivial to override a sender in tests.
        var map = new Dictionary<Channel, IChannelSender>();
        foreach (var sender in senders)
        {
            map[sender.Channel] = sender;
        }
        _byChannel = map;
    }

    public IChannelSender Resolve(Channel channel)
    {
        if (_byChannel.TryGetValue(channel, out var sender))
        {
            return sender;
        }
        throw new InvalidOperationException($"No sender registered for channel {channel}.");
    }
}
