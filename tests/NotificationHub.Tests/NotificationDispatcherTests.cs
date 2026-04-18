using Microsoft.Extensions.Logging.Abstractions;
using NotificationHub;
using Xunit;

namespace NotificationHub.Tests;

public class NotificationDispatcherTests
{
    [Fact]
    public async Task Matches_subscriptions_by_glob_pattern()
    {
        var (dispatcher, subs, deliveries, _) = BuildSut();
        await subs.AddAsync(
            Subscription.New("r1", "customs.*", Channel.Email, "r1@example.com"),
            CancellationToken.None);
        await subs.AddAsync(
            Subscription.New("r2", "sanctions.*", Channel.Email, "r2@example.com"),
            CancellationToken.None);

        var req = NotificationRequest.New(
            "customs.declaration.accepted",
            Severity.Info,
            "Declaration",
            "D-1",
            "subject",
            "body");

        var produced = await dispatcher.DispatchAsync(req, CancellationToken.None);

        Assert.Single(produced);
        Assert.Equal(DeliveryStatus.Delivered, produced[0].Status);
        var all = await deliveries.ForNotificationAsync(req.Id, CancellationToken.None);
        Assert.Single(all);
    }

    [Fact]
    public async Task Quiet_hours_suppress_non_critical_but_not_critical()
    {
        var (dispatcher, subs, _, _) = BuildSut();
        // Quiet window covering the full day in UTC.
        var alwaysQuiet = new QuietHours(
            new TimeOnly(0, 0),
            new TimeOnly(23, 59),
            "UTC");

        await subs.AddAsync(
            Subscription.New("r1", "customs.*", Channel.Email, "r1@example.com", alwaysQuiet),
            CancellationToken.None);

        var info = NotificationRequest.New(
            "customs.document.expiring",
            Severity.Info,
            "Document",
            "Doc-1",
            "s",
            "b");
        var producedInfo = await dispatcher.DispatchAsync(info, CancellationToken.None);
        Assert.Empty(producedInfo);

        var critical = NotificationRequest.New(
            "customs.sanctions.hit",
            Severity.Critical,
            "Declaration",
            "D-9",
            "s",
            "b");
        var producedCrit = await dispatcher.DispatchAsync(critical, CancellationToken.None);
        Assert.Single(producedCrit);
    }

    [Fact]
    public async Task Multiple_channels_for_one_recipient_all_fire()
    {
        var (dispatcher, subs, _, _) = BuildSut();
        await subs.AddAsync(
            Subscription.New("r1", "customs.duty.*", Channel.Email, "r1@example.com"),
            CancellationToken.None);
        await subs.AddAsync(
            Subscription.New("r1", "customs.duty.*", Channel.Sms, "+1000"),
            CancellationToken.None);
        await subs.AddAsync(
            Subscription.New("r1", "customs.duty.*", Channel.Webhook, "https://hook.example/x"),
            CancellationToken.None);

        var req = NotificationRequest.New(
            "customs.duty.assessed",
            Severity.Warning,
            "Declaration",
            "D-2",
            "subject",
            "body");

        var produced = await dispatcher.DispatchAsync(req, CancellationToken.None);

        Assert.Equal(3, produced.Count);
        Assert.Contains(produced, d => d.Channel == Channel.Email);
        Assert.Contains(produced, d => d.Channel == Channel.Sms);
        Assert.Contains(produced, d => d.Channel == Channel.Webhook);
        Assert.All(produced, d => Assert.Equal(DeliveryStatus.Delivered, d.Status));
    }

    [Fact]
    public async Task Critical_with_escalation_chain_creates_delivery_per_step()
    {
        var (dispatcher, subs, deliveries, router) = BuildSut(forceFailureFor: Channel.Sms);

        await subs.AddAsync(
            Subscription.New(
                "primary", "customs.sanctions.*", Channel.Sms, "+1111",
                escalationDelayMinutes: 5),
            CancellationToken.None);
        await subs.AddAsync(
            Subscription.New(
                "backup", "customs.sanctions.*", Channel.Email, "backup@example.com",
                escalationDelayMinutes: 15),
            CancellationToken.None);

        var req = NotificationRequest.New(
            "customs.sanctions.hit",
            Severity.Critical,
            "Declaration",
            "D-3",
            "s",
            "b");

        var produced = await dispatcher.DispatchAsync(req, CancellationToken.None);

        Assert.Equal(2, produced.Count);
        Assert.Contains(produced, d => d.Channel == Channel.Sms);
        Assert.Contains(produced, d => d.Channel == Channel.Email && d.Status == DeliveryStatus.Delivered);
    }

    [Fact]
    public async Task No_matching_subscription_produces_no_deliveries()
    {
        var (dispatcher, subs, _, _) = BuildSut();
        await subs.AddAsync(
            Subscription.New("r1", "sanctions.*", Channel.Email, "r@example.com"),
            CancellationToken.None);

        var req = NotificationRequest.New(
            "customs.declaration.accepted",
            Severity.Info,
            "Declaration",
            "D-1",
            "s",
            "b");

        var produced = await dispatcher.DispatchAsync(req, CancellationToken.None);

        Assert.Empty(produced);
    }

    private static (
        NotificationDispatcher dispatcher,
        ISubscriptionStore subs,
        IDeliveryStore deliveries,
        IChannelRouter router) BuildSut(Channel? forceFailureFor = null)
    {
        var subs = new InMemorySubscriptionStore();
        var deliveries = new InMemoryDeliveryStore();

        var senders = new List<IChannelSender>
        {
            new FakeSender(Channel.Email, succeed: forceFailureFor != Channel.Email),
            new FakeSender(Channel.Sms, succeed: forceFailureFor != Channel.Sms),
            new FakeSender(Channel.Webhook, succeed: forceFailureFor != Channel.Webhook),
            new FakeSender(Channel.InApp, succeed: forceFailureFor != Channel.InApp)
        };
        var router = new ChannelRouter(senders);
        // Tight policy so tests don't actually sleep for long.
        var retry = new RetryPolicy(
            maxAttempts: 2,
            baseDelay: TimeSpan.FromMilliseconds(1),
            maxDelay: TimeSpan.FromMilliseconds(2),
            rng: new Random(0));
        var escalation = new EscalationEngine(
            deliveries,
            router,
            NullLogger<EscalationEngine>.Instance);
        var dispatcher = new NotificationDispatcher(
            subs,
            deliveries,
            router,
            retry,
            escalation,
            NullLogger<NotificationDispatcher>.Instance);

        return (dispatcher, subs, deliveries, router);
    }

    private sealed class FakeSender : IChannelSender
    {
        private readonly bool _succeed;

        public FakeSender(Channel channel, bool succeed)
        {
            Channel = channel;
            _succeed = succeed;
        }

        public Channel Channel { get; }

        public Task<ChannelResult> SendAsync(Delivery delivery, CancellationToken ct) =>
            Task.FromResult(_succeed
                ? new ChannelResult(true, null)
                : new ChannelResult(false, "simulated failure"));
    }
}
