namespace NotificationHub;

// Mutable: a delivery's status, attempt count, and error string evolve over retries.
public sealed class Delivery
{
    public Guid Id { get; init; }
    public Guid NotificationId { get; init; }
    public Guid SubscriptionId { get; init; }
    public Channel Channel { get; init; }
    public string Address { get; init; } = string.Empty;
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;
    public int AttemptCount { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public string? LastError { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public static Delivery ForSubscription(NotificationRequest request, Subscription sub) =>
        new()
        {
            Id = Guid.NewGuid(),
            NotificationId = request.Id,
            SubscriptionId = sub.Id,
            Channel = sub.Channel,
            Address = sub.Address
        };
}
