using System.Collections.Concurrent;

namespace NotificationHub;

public sealed class InMemorySubscriptionStore : ISubscriptionStore
{
    private readonly ConcurrentDictionary<Guid, Subscription> _subs = new();

    public Task AddAsync(Subscription subscription, CancellationToken ct)
    {
        _subs[subscription.Id] = subscription;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Subscription>> ListAsync(CancellationToken ct)
    {
        IReadOnlyList<Subscription> snapshot = _subs.Values.ToList();
        return Task.FromResult(snapshot);
    }

    public Task<IReadOnlyList<Subscription>> MatchAsync(string eventType, CancellationToken ct)
    {
        IReadOnlyList<Subscription> matches = _subs.Values
            .Where(s => Glob.IsMatch(s.EventTypePattern, eventType))
            .ToList();
        return Task.FromResult(matches);
    }
}
