using Confluent.Kafka;
using FinancialAPI.Application.Interfaces.Kafka;
using FinancialAPI.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FinancialAPI.Infrastructure.Kafka;

/// <summary>
/// Generic Kafka producer using Confluent.Kafka.
/// - Serialises messages to JSON
/// - Wraps payload in KafkaMessage envelope
/// - Retries up to 3 times on transient failures
/// - Logs delivery confirmation and errors
/// </summary>
public class KafkaProducer<T> : IKafkaProducer<T>, IDisposable
    where T : class
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer<T>> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        WriteIndented               = false,
        DefaultIgnoreCondition      = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public KafkaProducer(
        IOptions<KafkaSettings> settings,
        ILogger<KafkaProducer<T>> logger)
    {
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers        = settings.Value.BootstrapServers,
            Acks                    = Acks.All,          // Wait for all replicas to acknowledge
            EnableIdempotence       = true,              // Exactly-once semantics
            MaxInFlight             = 5,
            MessageSendMaxRetries   = 3,
            RetryBackoffMs          = 1000,
            CompressionType         = CompressionType.Snappy,
            LingerMs                = 5,                 // Small batching window
            BatchSize               = 16384,
        };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
                _logger.LogError("Kafka producer error: {Reason} (IsFatal: {IsFatal})",
                    error.Reason, error.IsFatal))
            .SetLogHandler((_, log) =>
                _logger.LogDebug("Kafka producer log [{Level}]: {Message}",
                    log.Level, log.Message))
            .Build();
    }

    public async Task ProduceAsync(
        string topic,
        string key,
        T message,
        CancellationToken ct = default)
    {
        // Wrap in envelope
        var envelope = KafkaMessage<T>.Create(typeof(T).Name, message);
        var json     = JsonSerializer.Serialize(envelope, _jsonOptions);

        var kafkaMessage = new Message<string, string>
        {
            Key   = key,
            Value = json,
            Headers = new Headers
            {
                { "message-id", System.Text.Encoding.UTF8.GetBytes(envelope.MessageId.ToString()) },
                { "event-type", System.Text.Encoding.UTF8.GetBytes(envelope.EventType) },
                { "source",     System.Text.Encoding.UTF8.GetBytes(envelope.Source) }
            }
        };

        const int maxRetries = 3;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var result = await _producer.ProduceAsync(topic, kafkaMessage, ct);

                _logger.LogInformation(
                    "Kafka message produced. Topic: {Topic}, Partition: {Partition}, Offset: {Offset}, Key: {Key}",
                    result.TopicPartitionOffset.Topic,
                    result.TopicPartitionOffset.Partition.Value,
                    result.TopicPartitionOffset.Offset.Value,
                    key);

                return; // Success — exit retry loop
            }
            catch (ProduceException<string, string> ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(
                    "Kafka produce attempt {Attempt}/{Max} failed for topic {Topic}. Error: {Error}. Retrying...",
                    attempt, maxRetries, topic, ex.Error.Reason);

                await Task.Delay(TimeSpan.FromSeconds(attempt * 2), ct); // Exponential back-off
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogError(ex,
                    "Kafka produce failed after {Max} attempts for topic {Topic}. Key: {Key}",
                    maxRetries, topic, key);
                throw;
            }
        }
    }

    public void Dispose()
    {
        // Flush ensures any buffered messages are sent before shutdown
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}
