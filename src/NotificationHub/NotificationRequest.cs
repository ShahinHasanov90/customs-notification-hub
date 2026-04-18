namespace NotificationHub;

public sealed record NotificationRequest(
    Guid Id,
    string EventType,
    Severity Severity,
    string EntityType,
    string EntityId,
    IReadOnlyDictionary<string, string> Payload,
    string Subject,
    string Body,
    DateTime OccurredAt)
{
    public static NotificationRequest New(
        string eventType,
        Severity severity,
        string entityType,
        string entityId,
        string subject,
        string body,
        IReadOnlyDictionary<string, string>? payload = null,
        DateTime? occurredAt = null) =>
        new(
            Guid.NewGuid(),
            eventType,
            severity,
            entityType,
            entityId,
            payload ?? new Dictionary<string, string>(),
            subject,
            body,
            occurredAt ?? DateTime.UtcNow);
}
