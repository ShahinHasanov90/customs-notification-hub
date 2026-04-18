using NotificationHub;
using NotificationHub.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ISubscriptionStore, InMemorySubscriptionStore>();
builder.Services.AddSingleton<IDeliveryStore, InMemoryDeliveryStore>();
builder.Services.AddSingleton<IChannelSender, SmtpChannelSender>();
builder.Services.AddSingleton<IChannelSender, SmsChannelSender>();
builder.Services.AddSingleton<IChannelSender, WebhookChannelSender>();
builder.Services.AddSingleton<IChannelSender, InAppChannelSender>();
builder.Services.AddSingleton<IChannelRouter>(sp =>
    new ChannelRouter(sp.GetServices<IChannelSender>()));
builder.Services.AddSingleton(new RetryPolicy());
builder.Services.AddSingleton<EscalationEngine>();
builder.Services.AddSingleton<NotificationDispatcher>();

var app = builder.Build();

await SubscriptionSeed.SeedAsync(app.Services);

app.MapPost("/notifications", async (
    NotificationRequestDto dto,
    NotificationDispatcher dispatcher,
    CancellationToken ct) =>
{
    var request = NotificationRequest.New(
        dto.EventType,
        dto.Severity,
        dto.EntityType,
        dto.EntityId,
        dto.Subject,
        dto.Body,
        dto.Payload,
        dto.OccurredAt);

    var deliveries = await dispatcher.DispatchAsync(request, ct);
    return Results.Accepted(
        $"/notifications/{request.Id}/deliveries",
        new
        {
            notificationId = request.Id,
            deliveryIds = deliveries.Select(d => d.Id).ToArray()
        });
});

app.MapGet("/notifications/{id:guid}/deliveries", async (
    Guid id,
    IDeliveryStore store,
    CancellationToken ct) =>
{
    var rows = await store.ForNotificationAsync(id, ct);
    return Results.Ok(rows.Select(DeliveryDto.From).ToArray());
});

app.MapPost("/subscriptions", async (
    SubscriptionDto dto,
    ISubscriptionStore store,
    CancellationToken ct) =>
{
    QuietHours? qh = null;
    if (dto.QuietHoursStart is not null && dto.QuietHoursEnd is not null && dto.QuietHoursTimeZone is not null)
    {
        qh = new QuietHours(
            TimeOnly.Parse(dto.QuietHoursStart),
            TimeOnly.Parse(dto.QuietHoursEnd),
            dto.QuietHoursTimeZone);
    }

    var sub = Subscription.New(
        dto.RecipientId,
        dto.EventTypePattern,
        dto.Channel,
        dto.Address,
        qh,
        dto.EscalationDelayMinutes ?? 0);

    await store.AddAsync(sub, ct);
    return Results.Created($"/subscriptions/{sub.Id}", new { id = sub.Id });
});

app.MapGet("/subscriptions", async (ISubscriptionStore store, CancellationToken ct) =>
{
    var all = await store.ListAsync(ct);
    return Results.Ok(all);
});

app.Run();

public partial class Program;
