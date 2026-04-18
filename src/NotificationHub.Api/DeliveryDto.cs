namespace NotificationHub.Api;

public sealed record DeliveryDto(
    Guid Id,
    Guid NotificationId,
    Guid SubscriptionId,
    Channel Channel,
    string Address,
    DeliveryStatus Status,
    int AttemptCount,
    DateTime? LastAttemptAt,
    string? LastError,
    DateTime? DeliveredAt,
    DateTime CreatedAt)
{
    public static DeliveryDto From(Delivery d) =>
        new(
            d.Id,
            d.NotificationId,
            d.SubscriptionId,
            d.Channel,
            d.Address,
            d.Status,
            d.AttemptCount,
            d.LastAttemptAt,
            d.LastError,
            d.DeliveredAt,
            d.CreatedAt);
}
