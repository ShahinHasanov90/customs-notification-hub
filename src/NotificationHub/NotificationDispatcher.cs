using Microsoft.Extensions.Logging;

namespace NotificationHub;

public sealed class NotificationDispatcher
{
    private readonly ISubscriptionStore _subscriptions;
    private readonly IDeliveryStore _deliveries;
    private readonly IChannelRouter _router;
    private readonly RetryPolicy _retry;
    private readonly EscalationEngine _escalation;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        ISubscriptionStore subscriptions,
        IDeliveryStore deliveries,
        IChannelRouter router,
        RetryPolicy retry,
        EscalationEngine escalation,
        ILogger<NotificationDispatcher> logger)
    {
        _subscriptions = subscriptions;
        _deliveries = deliveries;
        _router = router;
        _retry = retry;
        _escalation = escalation;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Delivery>> DispatchAsync(
        NotificationRequest request,
        CancellationToken ct)
    {
        var nowUtc = DateTime.UtcNow;
        var matched = await _subscriptions.MatchAsync(request.EventType, ct);

        // Non-critical traffic honours quiet hours; Critical events bypass them.
        var eligible = matched
            .Where(s => request.Severity == Severity.Critical
                        || s.QuietHours is null
                        || !s.QuietHours.IsWithin(nowUtc))
            .ToList();

        if (request.Severity == Severity.Critical && HasEscalationChain(eligible))
        {
            var chain = eligible
                .OrderBy(s => s.EscalationDelayMinutes)
                .ThenBy(s => s.Id)
                .ToList();
            return await _escalation.RunAsync(request, chain, nowUtc, ct);
        }

        var produced = new List<Delivery>();
        foreach (var sub in eligible)
        {
            var delivery = Delivery.ForSubscription(request, sub);
            await _deliveries.AddAsync(delivery, ct);
            produced.Add(delivery);

            await TrySendWithRetriesAsync(delivery, ct);
        }

        _logger.LogInformation(
            "Dispatched notification {NotificationId} ({EventType}) to {Count} subscriber(s)",
            request.Id,
            request.EventType,
            produced.Count);

        return produced;
    }

    private static bool HasEscalationChain(IReadOnlyList<Subscription> subs) =>
        subs.Any(s => s.EscalationDelayMinutes > 0);

    private async Task TrySendWithRetriesAsync(Delivery delivery, CancellationToken ct)
    {
        while (true)
        {
            delivery.Status = DeliveryStatus.Sending;
            delivery.AttemptCount++;
            delivery.LastAttemptAt = DateTime.UtcNow;

            ChannelResult result;
            try
            {
                var sender = _router.Resolve(delivery.Channel);
                result = await sender.SendAsync(delivery, ct);
            }
            catch (Exception ex)
            {
                result = new ChannelResult(false, ex.Message);
            }

            if (result.Success)
            {
                delivery.Status = DeliveryStatus.Delivered;
                delivery.DeliveredAt = DateTime.UtcNow;
                delivery.LastError = null;
                await _deliveries.UpdateAsync(delivery, ct);
                return;
            }

            delivery.LastError = result.Error;
            if (!_retry.ShouldRetry(delivery.AttemptCount))
            {
                delivery.Status = DeliveryStatus.Abandoned;
                await _deliveries.UpdateAsync(delivery, ct);
                return;
            }

            delivery.Status = DeliveryStatus.Failed;
            await _deliveries.UpdateAsync(delivery, ct);

            // The reference implementation retries inline. A production build would
            // enqueue for a worker so that the caller never blocks on backoff.
            var delay = _retry.NextDelay(delivery.AttemptCount, out _);
            if (delay > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(delay, ct);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }
}
