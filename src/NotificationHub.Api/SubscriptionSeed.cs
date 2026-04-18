namespace NotificationHub.Api;

internal static class SubscriptionSeed
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var store = services.GetRequiredService<ISubscriptionStore>();

        var samples = new[]
        {
            Subscription.New(
                recipientId: "importer-1",
                eventTypePattern: "customs.declaration.*",
                channel: Channel.Email,
                address: "importer@example.com"),
            Subscription.New(
                recipientId: "broker-1",
                eventTypePattern: "customs.*",
                channel: Channel.Webhook,
                address: "https://broker.example.com/hooks/customs"),
            Subscription.New(
                recipientId: "compliance-1",
                eventTypePattern: "customs.sanctions.*",
                channel: Channel.Sms,
                address: "+10000000000",
                escalationDelayMinutes: 5),
            Subscription.New(
                recipientId: "officer-on-call",
                eventTypePattern: "customs.sanctions.*",
                channel: Channel.Email,
                address: "oncall@customs.example",
                escalationDelayMinutes: 15)
        };

        foreach (var s in samples)
        {
            await store.AddAsync(s, CancellationToken.None);
        }
    }
}
