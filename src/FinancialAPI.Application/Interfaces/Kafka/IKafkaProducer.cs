namespace FinancialAPI.Application.Interfaces.Kafka;

/// <summary>
/// Generic Kafka producer interface.
/// Decouples application services from the Confluent.Kafka library.
/// Usage: inject IKafkaProducer&lt;PaymentEvent&gt; and call ProduceAsync()
/// </summary>
public interface IKafkaProducer<T> where T : class
{
    /// <summary>
    /// Publish a message to a Kafka topic.
    /// </summary>
    /// <param name="topic">Target Kafka topic name</param>
    /// <param name="key">Partition key — use entity ID for ordering guarantees</param>
    /// <param name="message">The message object (serialised to JSON internally)</param>
    /// <param name="ct">Cancellation token</param>
    Task ProduceAsync(string topic, string key, T message, CancellationToken ct = default);
}
