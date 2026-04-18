using Microsoft.Extensions.Logging;

namespace NotificationHub;

public sealed class WebhookChannelSender : IChannelSender
{
    private readonly ILogger<WebhookChannelSender> _logger;

    public WebhookChannelSender(ILogger<WebhookChannelSender> logger)
    {
        _logger = logger;
    }

    public Channel Channel => Channel.Webhook;

    public Task<ChannelResult> SendAsync(Delivery delivery, CancellationToken ct)
    {
        _logger.LogInformation(
            "Webhook dispatch stub: notif={NotificationId} to={Address}",
            delivery.NotificationId,
            delivery.Address);
        return Task.FromResult(new ChannelResult(true, null));
    }
}
