namespace NotificationHub.Api;

public sealed record SubscriptionDto(
    string RecipientId,
    string EventTypePattern,
    Channel Channel,
    string Address,
    string? QuietHoursStart,
    string? QuietHoursEnd,
    string? QuietHoursTimeZone,
    int? EscalationDelayMinutes);
