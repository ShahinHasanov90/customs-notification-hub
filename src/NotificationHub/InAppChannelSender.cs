using Microsoft.Extensions.Logging;

namespace NotificationHub;

public sealed class InAppChannelSender : IChannelSender
{
    private readonly ILogger<InAppChannelSender> _logger;

    public InAppChannelSender(ILogger<InAppChannelSender> logger)
    {
        _logger = logger;
    }

    public Channel Channel => Channel.InApp;

    public Task<ChannelResult> SendAsync(Delivery delivery, CancellationToken ct)
    {
        _logger.LogInformation(
            "In-app dispatch stub: notif={NotificationId} to={Address}",
            delivery.NotificationId,
            delivery.Address);
        return Task.FromResult(new ChannelResult(true, null));
    }
}
