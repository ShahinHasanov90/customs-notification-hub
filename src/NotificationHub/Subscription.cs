namespace NotificationHub;

public sealed record Subscription(
    Guid Id,
    string RecipientId,
    string EventTypePattern,
    Channel Channel,
    string Address,
    QuietHours? QuietHours,
    int EscalationDelayMinutes)
{
    public static Subscription New(
        string recipientId,
        string eventTypePattern,
        Channel channel,
        string address,
        QuietHours? quietHours = null,
        int escalationDelayMinutes = 0) =>
        new(
            Guid.NewGuid(),
            recipientId,
            eventTypePattern,
            channel,
            address,
            quietHours,
            escalationDelayMinutes);
}
