using Confluent.Kafka;
using FinancialAPI.Shared;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace FinancialAPI.Infrastructure.Kafka;

/// <summary>
/// Health check for Kafka broker connectivity.
/// Exposed at GET /health — returns Healthy/Unhealthy.
/// </summary>
public class KafkaHealthCheck : IHealthCheck
{
    private readonly KafkaSettings _settings;

    public KafkaHealthCheck(IOptions<KafkaSettings> settings)
        => _settings = settings.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var config = new AdminClientConfig
            {
                BootstrapServers = _settings.BootstrapServers
            };

            using var adminClient = new AdminClientBuilder(config).Build();

            // Try to get metadata — proves broker is reachable
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(5));

            return HealthCheckResult.Healthy(
                $"Kafka connected. Brokers: {metadata.Brokers.Count}, Topics: {metadata.Topics.Count}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"Kafka unreachable: {ex.Message}", ex);
        }
    }
}
