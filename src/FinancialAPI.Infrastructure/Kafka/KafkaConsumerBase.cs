using Confluent.Kafka;
using FinancialAPI.Application.Interfaces.Kafka;
using FinancialAPI.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FinancialAPI.Infrastructure.Kafka;

/// <summary>
/// Abstract base class for all Kafka consumers.
/// Subclasses only need to specify the topic name — all plumbing is handled here.
///
/// Pattern:
///   KafkaConsumerBase[T] → deserialises message → calls IKafkaConsumerHandler[T]
///
/// Features:
///   - Graceful shutdown via CancellationToken
///   - Dead-letter logging on deserialisation or handler failure
///   - Manual offset commit (at-least-once delivery)
///   - Uses IServiceScopeFactory so handlers can use scoped services (e.g. DbContext)
/// </summary>
public abstract class KafkaConsumerBase<T> : BackgroundService
    where T : class
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;
    private readonly KafkaSettings _settings;
    private readonly string _topic;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected KafkaConsumerBase(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaSettings> settings,
        ILogger logger,
        string topic)
    {
        _scopeFactory = scopeFactory;
        _settings     = settings.Value;
        _logger       = logger;
        _topic        = topic;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Kafka consumer starting. Topic: {Topic}, Group: {Group}",
            _topic, _settings.ConsumerGroupId);

        var config = new ConsumerConfig
        {
            BootstrapServers       = _settings.BootstrapServers,
            GroupId                = _settings.ConsumerGroupId,
            AutoOffsetReset        = AutoOffsetReset.Earliest,
            EnableAutoCommit       = false,   // Manual commit for reliability
            EnableAutoOffsetStore  = false,
            MaxPollIntervalMs      = 300000,  // 5 minutes
            SessionTimeoutMs       = 45000,
            HeartbeatIntervalMs    = 3000,
            IsolationLevel         = IsolationLevel.ReadCommitted,
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
                _logger.LogError("Kafka consumer error: {Reason}", error.Reason))
            .SetPartitionsAssignedHandler((_, partitions) =>
                _logger.LogInformation("Kafka partitions assigned: {Partitions}",
                    string.Join(", ", partitions)))
            .SetPartitionsRevokedHandler((_, partitions) =>
                _logger.LogInformation("Kafka partitions revoked: {Partitions}",
                    string.Join(", ", partitions)))
            .Build();

        consumer.Subscribe(_topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result = null;
                try
                {
                    // Non-blocking consume with 1s timeout so we can check cancellation
                    result = consumer.Consume(stoppingToken);

                    if (result is null || result.IsPartitionEOF) continue;

                    _logger.LogDebug(
                        "Message received. Topic: {Topic}, Partition: {Partition}, Offset: {Offset}, Key: {Key}",
                        result.Topic, result.Partition.Value, result.Offset.Value, result.Message.Key);

                    // Deserialise envelope
                    var envelope = JsonSerializer.Deserialize<KafkaMessage<T>>(
                        result.Message.Value, _jsonOptions);

                    if (envelope?.Payload is null)
                    {
                        _logger.LogWarning(
                            "Null payload after deserialisation. Topic: {Topic}, Offset: {Offset}",
                            _topic, result.Offset.Value);
                        CommitOffset(consumer, result);
                        continue;
                    }

                    // Resolve handler in a fresh DI scope (supports DbContext, etc.)
                    using var scope   = _scopeFactory.CreateScope();
                    var handler       = scope.ServiceProvider
                                            .GetRequiredService<IKafkaConsumerHandler<T>>();

                    await handler.HandleAsync(envelope.Payload, stoppingToken);

                    // Only commit after successful processing
                    CommitOffset(consumer, result);
                }
                catch (OperationCanceledException)
                {
                    // Normal shutdown — break gracefully
                    break;
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex,
                        "Kafka consume error on topic {Topic}: {Reason}",
                        _topic, ex.Error.Reason);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Unhandled error processing message from topic {Topic}, Offset: {Offset}",
                        _topic, result?.Offset.Value);

                    // Dead-letter: log the raw message so it can be replayed manually
                    if (result is not null)
                    {
                        _logger.LogError(
                            "DEAD-LETTER | Topic: {Topic} | Key: {Key} | Value: {Value}",
                            result.Topic, result.Message.Key, result.Message.Value);

                        CommitOffset(consumer, result); // Skip poison message to avoid infinite loop
                    }
                }
            }
        }
        finally
        {
            consumer.Close(); // Triggers rebalance so other consumers pick up partitions
            _logger.LogInformation("Kafka consumer stopped. Topic: {Topic}", _topic);
        }
    }

    private void CommitOffset(IConsumer<string, string> consumer, ConsumeResult<string, string> result)
    {
        try
        {
            consumer.StoreOffset(result);
            consumer.Commit(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to commit Kafka offset for topic {Topic}", _topic);
        }
    }
}
