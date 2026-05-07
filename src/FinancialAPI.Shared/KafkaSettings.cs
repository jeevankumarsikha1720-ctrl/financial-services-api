namespace FinancialAPI.Shared;

/// <summary>
/// Strongly-typed Kafka configuration — bound from appsettings.json "Kafka" section.
/// </summary>
public class KafkaSettings
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ConsumerGroupId  { get; set; } = "financial-api-group";
    public string SecurityProtocol { get; set; } = "PLAINTEXT";

    public KafkaTopics Topics { get; set; } = new();
}

public class KafkaTopics
{
    public string PaymentEvents      { get; set; } = "payment-events";
    public string SettlementEvents   { get; set; } = "settlement-events";
    public string BeneficiaryEvents  { get; set; } = "beneficiary-events";
    public string LedgerEvents       { get; set; } = "ledger-events";
    public string FraudAlerts        { get; set; } = "fraud-alerts";
    public string NotificationEvents { get; set; } = "notification-events";
}
