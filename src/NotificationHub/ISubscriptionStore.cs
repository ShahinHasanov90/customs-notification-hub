namespace NotificationHub;

public interface ISubscriptionStore
{
    Task AddAsync(Subscription subscription, CancellationToken ct);

    Task<IReadOnlyList<Subscription>> ListAsync(CancellationToken ct);

    Task<IReadOnlyList<Subscription>> MatchAsync(string eventType, CancellationToken ct);
}
