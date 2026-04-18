namespace NotificationHub.Api;

public sealed record NotificationRequestDto(
    string EventType,
    Severity Severity,
    string EntityType,
    string EntityId,
    string Subject,
    string Body,
    Dictionary<string, string>? Payload,
    DateTime? OccurredAt);
