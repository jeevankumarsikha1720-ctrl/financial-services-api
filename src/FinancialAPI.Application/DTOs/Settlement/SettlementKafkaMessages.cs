using FinancialAPI.Domain.Enums;

namespace FinancialAPI.Application.DTOs.Settlement;

public record SettlementCreatedMessage
{
    public Guid SettlementId { get; init; }
    public string BatchReference { get; init; } = string.Empty;
    public CurrencyCode Currency { get; init; }
    public DateTime SettlementDate { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record SettlementCompletedMessage
{
    public Guid SettlementId { get; init; }
    public string BatchReference { get; init; } = string.Empty;
    public decimal TotalGrossAmount { get; init; }
    public decimal TotalFees { get; init; }
    public decimal NetSettlementAmount { get; init; }
    public int TotalTransactionCount { get; init; }
    public int SuccessfulCount { get; init; }
    public int FailedCount { get; init; }
    public string ReconciliationReference { get; init; } = string.Empty;
    public DateTime CompletedAt { get; init; }
}
