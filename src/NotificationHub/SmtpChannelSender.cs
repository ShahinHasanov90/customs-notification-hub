using Microsoft.Extensions.Logging;

namespace NotificationHub;

public sealed class SmtpChannelSender : IChannelSender
{
    private readonly ILogger<SmtpChannelSender> _logger;

    public SmtpChannelSender(ILogger<SmtpChannelSender> logger)
    {
        _logger = logger;
    }

    public Channel Channel => Channel.Email;

    public Task<ChannelResult> SendAsync(Delivery delivery, CancellationToken ct)
    {
        _logger.LogInformation(
            "SMTP dispatch stub: notif={NotificationId} to={Address}",
            delivery.NotificationId,
            delivery.Address);
        return Task.FromResult(new ChannelResult(true, null));
    }
}
