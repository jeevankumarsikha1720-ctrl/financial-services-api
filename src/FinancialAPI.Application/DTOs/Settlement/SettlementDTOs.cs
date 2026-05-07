using System.ComponentModel.DataAnnotations;
using FinancialAPI.Domain.Enums;

namespace FinancialAPI.Application.DTOs.Settlement;

// ── Request DTOs ──────────────────────────────────────────────────────────────

public record CreateSettlementRequest
{
    [Required]
    public CurrencyCode Currency { get; init; }

    [Required]
    public DateTime SettlementDate { get; init; }
}

public record AddSettlementEntryRequest
{
    [Required]
    public Guid PaymentId { get; init; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal GrossAmount { get; init; }

    [Range(0, double.MaxValue)]
    public decimal Fee { get; init; } = 0m;
}

public record CompleteSettlementRequest
{
    [Required]
    public string ReconciliationReference { get; init; } = string.Empty;
}

public record ReconcileSettlementRequest
{
    [Required]
    public string Notes { get; init; } = string.Empty;
}

public record GetSettlementsQuery
{
    public SettlementStatus? Status { get; init; }
    public CurrencyCode? Currency { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

// ── Response DTOs ─────────────────────────────────────────────────────────────

public record SettlementEntryResponse
{
    public Guid Id { get; init; }
    public Guid PaymentId { get; init; }
    public decimal GrossAmount { get; init; }
    public decimal Fee { get; init; }
    public decimal NetAmount { get; init; }
    public bool IsFailed { get; init; }
    public string? FailureReason { get; init; }
}

public record SettlementResponse
{
    public Guid Id { get; init; }
    public string BatchReference { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public DateTime SettlementDate { get; init; }
    public decimal TotalGrossAmount { get; init; }
    public decimal TotalFees { get; init; }
    public decimal NetSettlementAmount { get; init; }
    public int TotalTransactionCount { get; init; }
    public int SuccessfulCount { get; init; }
    public int FailedCount { get; init; }
    public string? ReconciliationReference { get; init; }
    public DateTime? ReconciledAt { get; init; }
    public string? Notes { get; init; }
    public string? FailureReason { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public IReadOnlyList<SettlementEntryResponse> Entries { get; init; } = [];
}

public record SettlementListResponse
{
    public IReadOnlyList<SettlementResponse> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
