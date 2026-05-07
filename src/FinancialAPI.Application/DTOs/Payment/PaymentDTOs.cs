using System.ComponentModel.DataAnnotations;
using FinancialAPI.Domain.Enums;

namespace FinancialAPI.Application.DTOs.Payment;

// ── Request DTOs ──────────────────────────────────────────────────────────────

/// <summary>Request body for initiating a new payment.</summary>
public record InitiatePaymentRequest
{
    [Required]
    public string SenderAccountId { get; init; } = string.Empty;

    [Required]
    public string SenderName { get; init; } = string.Empty;

    [Required]
    public Guid BeneficiaryId { get; init; }

    [Required]
    public string BeneficiaryAccountNumber { get; init; } = string.Empty;

    [Required]
    [StringLength(11, MinimumLength = 8)]
    public string BeneficiaryBankCode { get; init; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; init; }

    [Required]
    public CurrencyCode Currency { get; init; }

    [Required]
    public PaymentType Type { get; init; }

    [StringLength(500)]
    public string Description { get; init; } = string.Empty;
}

/// <summary>Request body for updating payment status manually.</summary>
public record UpdatePaymentStatusRequest
{
    [Required]
    public string Status { get; init; } = string.Empty;

    public string? Reason { get; init; }
}

/// <summary>Query parameters for listing payments.</summary>
public record GetPaymentsQuery
{
    public string? SenderAccountId { get; init; }
    public PaymentStatus? Status { get; init; }
    public PaymentType? Type { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

// ── Response DTOs ─────────────────────────────────────────────────────────────

/// <summary>Full payment details returned by API.</summary>
public record PaymentResponse
{
    public Guid Id { get; init; }
    public string ReferenceNumber { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string SenderAccountId { get; init; } = string.Empty;
    public string SenderName { get; init; } = string.Empty;
    public Guid BeneficiaryId { get; init; }
    public string BeneficiaryAccountNumber { get; init; } = string.Empty;
    public string BeneficiaryBankCode { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? FailureReason { get; init; }
    public int RetryCount { get; init; }
    public double FraudScore { get; init; }
    public bool IsFlagged { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public DateTime? SettledAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>Paginated list of payments.</summary>
public record PaymentListResponse
{
    public IReadOnlyList<PaymentResponse> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
