namespace FinancialAPI.Shared;

/// <summary>
/// Standard envelope that wraps every Kafka message.
/// Adds tracing metadata without changing the payload.
/// </summary>
public class KafkaMessage<T> where T : class
{
    public Guid   MessageId   { get; init; } = Guid.NewGuid();
    public string EventType   { get; init; } = string.Empty;
    public string Source      { get; init; } = "FinancialAPI";
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public int    Version     { get; init; } = 1;
    public T      Payload     { get; init; } = null!;

    public static KafkaMessage<T> Create(string eventType, T payload) =>
        new() { EventType = eventType, Payload = payload };
}
