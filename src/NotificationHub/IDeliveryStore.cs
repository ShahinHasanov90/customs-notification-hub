namespace NotificationHub;

public interface IDeliveryStore
{
    Task AddAsync(Delivery delivery, CancellationToken ct);

    Task UpdateAsync(Delivery delivery, CancellationToken ct);

    Task<Delivery?> GetAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<Delivery>> ForNotificationAsync(Guid notificationId, CancellationToken ct);
}
