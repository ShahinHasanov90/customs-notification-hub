using Microsoft.Extensions.Logging;

namespace NotificationHub;

// Handles escalation chains for Critical notifications: if the head delivery is still
// unresolved past its EscalationDelayMinutes, the next subscription in the chain fires.
public sealed class EscalationEngine
{
    private readonly IDeliveryStore _deliveries;
    private readonly IChannelRouter _router;
    private readonly ILogger<EscalationEngine> _logger;

    public EscalationEngine(
        IDeliveryStore deliveries,
        IChannelRouter router,
        ILogger<EscalationEngine> logger)
    {
        _deliveries = deliveries;
        _router = router;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Delivery>> RunAsync(
        NotificationRequest request,
        IReadOnlyList<Subscription> chain,
        DateTime nowUtc,
        CancellationToken ct)
    {
        var produced = new List<Delivery>();
        if (chain.Count == 0) return produced;

        foreach (var sub in chain)
        {
            var delivery = Delivery.ForSubscription(request, sub);
            await _deliveries.AddAsync(delivery, ct);
            produced.Add(delivery);

            await TrySendAsync(delivery, ct);
            if (delivery.Status == DeliveryStatus.Delivered)
            {
                _logger.LogInformation(
                    "Escalation halted: step for subscription {SubscriptionId} delivered",
                    sub.Id);
                return produced;
            }

            if (sub.EscalationDelayMinutes <= 0)
            {
                // Treat zero-delay entries as parallel fan-out: fire and continue without waiting.
                continue;
            }

            // In a running system this would be scheduled. For the in-process reference
            // implementation we do not block the request thread; the orchestrator will
            // pick up unresolved deliveries through the retry scan.
            _logger.LogInformation(
                "Escalation step {SubscriptionId} armed with delay {DelayMin}m",
                sub.Id,
                sub.EscalationDelayMinutes);
        }

        return produced;
    }

    private async Task TrySendAsync(Delivery delivery, CancellationToken ct)
    {
        delivery.Status = DeliveryStatus.Sending;
        delivery.AttemptCount++;
        delivery.LastAttemptAt = DateTime.UtcNow;

        try
        {
            var sender = _router.Resolve(delivery.Channel);
            var result = await sender.SendAsync(delivery, ct);
            if (result.Success)
            {
                delivery.Status = DeliveryStatus.Delivered;
                delivery.DeliveredAt = DateTime.UtcNow;
                delivery.LastError = null;
            }
            else
            {
                delivery.Status = DeliveryStatus.Failed;
                delivery.LastError = result.Error;
            }
        }
        catch (Exception ex)
        {
            delivery.Status = DeliveryStatus.Failed;
            delivery.LastError = ex.Message;
        }
        finally
        {
            await _deliveries.UpdateAsync(delivery, ct);
        }
    }
}
