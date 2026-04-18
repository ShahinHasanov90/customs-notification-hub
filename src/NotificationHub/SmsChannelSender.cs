using Microsoft.Extensions.Logging;

namespace NotificationHub;

public sealed class SmsChannelSender : IChannelSender
{
    private readonly ILogger<SmsChannelSender> _logger;

    public SmsChannelSender(ILogger<SmsChannelSender> logger)
    {
        _logger = logger;
    }

    public Channel Channel => Channel.Sms;

    public Task<ChannelResult> SendAsync(Delivery delivery, CancellationToken ct)
    {
        _logger.LogInformation(
            "SMS dispatch stub: notif={NotificationId} to={Address}",
            delivery.NotificationId,
            delivery.Address);
        return Task.FromResult(new ChannelResult(true, null));
    }
}
