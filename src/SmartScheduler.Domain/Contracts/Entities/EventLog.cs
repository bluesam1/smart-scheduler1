namespace SmartScheduler.Domain.Contracts.Entities;

/// <summary>
/// Event log entity for audit trail of domain events.
/// </summary>
public class EventLog
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = string.Empty;
    public DateTime PublishedAt { get; private set; }
    public string PublishedToJson { get; private set; } = string.Empty; // JSON array of SignalR groups
    public DateTime CreatedAt { get; private set; }

    // Private constructor for EF Core
    private EventLog() { }

    public EventLog(
        Guid id,
        string eventType,
        string payloadJson,
        DateTime publishedAt,
        IReadOnlyList<string> publishedTo)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type cannot be empty.", nameof(eventType));

        if (string.IsNullOrWhiteSpace(payloadJson))
            throw new ArgumentException("Payload JSON cannot be empty.", nameof(payloadJson));

        Id = id;
        EventType = eventType;
        PayloadJson = payloadJson;
        PublishedAt = publishedAt;
        PublishedToJson = System.Text.Json.JsonSerializer.Serialize(publishedTo ?? Array.Empty<string>());
        CreatedAt = DateTime.UtcNow;
    }

    public IReadOnlyList<string> GetPublishedTo()
    {
        if (string.IsNullOrWhiteSpace(PublishedToJson))
            return Array.Empty<string>();

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<IReadOnlyList<string>>(PublishedToJson) ?? Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}


