using System.Collections.Concurrent;

namespace NotificationHub;

public sealed class InMemoryDeliveryStore : IDeliveryStore
{
    private readonly ConcurrentDictionary<Guid, Delivery> _deliveries = new();

    public Task AddAsync(Delivery delivery, CancellationToken ct)
    {
        _deliveries[delivery.Id] = delivery;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Delivery delivery, CancellationToken ct)
    {
        _deliveries[delivery.Id] = delivery;
        return Task.CompletedTask;
    }

    public Task<Delivery?> GetAsync(Guid id, CancellationToken ct)
    {
        _deliveries.TryGetValue(id, out var d);
        return Task.FromResult(d);
    }

    public Task<IReadOnlyList<Delivery>> ForNotificationAsync(Guid notificationId, CancellationToken ct)
    {
        IReadOnlyList<Delivery> matches = _deliveries.Values
            .Where(d => d.NotificationId == notificationId)
            .OrderBy(d => d.CreatedAt)
            .ToList();
        return Task.FromResult(matches);
    }
}
