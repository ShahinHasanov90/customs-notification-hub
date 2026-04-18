namespace NotificationHub;

public sealed record ChannelResult(bool Success, string? Error);

public interface IChannelSender
{
    Channel Channel { get; }

    Task<ChannelResult> SendAsync(Delivery delivery, CancellationToken ct);
}
