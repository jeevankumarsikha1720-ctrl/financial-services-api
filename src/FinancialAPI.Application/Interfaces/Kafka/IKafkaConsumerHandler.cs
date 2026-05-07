namespace FinancialAPI.Application.Interfaces.Kafka;

/// <summary>
/// Handler interface implemented by domain services that process Kafka messages.
/// Each consumer background service calls HandleAsync() for every message it receives.
/// </summary>
public interface IKafkaConsumerHandler<T> where T : class
{
    Task HandleAsync(T message, CancellationToken ct = default);
}
