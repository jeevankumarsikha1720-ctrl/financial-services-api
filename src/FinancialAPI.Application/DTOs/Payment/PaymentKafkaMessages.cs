using FinancialAPI.Domain.Enums;

namespace FinancialAPI.Application.DTOs.Payment;

/// <summary>Published to "payment-events" topic when payment is initiated.</summary>
public record PaymentInitiatedMessage
{
    public Guid PaymentId { get; init; }
    public string ReferenceNumber { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public CurrencyCode Currency { get; init; }
    public PaymentType PaymentType { get; init; }
    public string SenderAccountId { get; init; } = string.Empty;
    public string SenderName { get; init; } = string.Empty;
    public Guid BeneficiaryId { get; init; }
    public string BeneficiaryAccountNumber { get; init; } = string.Empty;
    public string BeneficiaryBankCode { get; init; } = string.Empty;
    public DateTime InitiatedAt { get; init; }
}

/// <summary>Published to "payment-events" topic when payment status changes.</summary>
public record PaymentStatusChangedMessage
{
    public Guid PaymentId { get; init; }
    public string ReferenceNumber  { get; init; } = string.Empty;
    public string OwnerId          { get; init; } = string.Empty;   // user who owns the payment
    public string SenderAccountId  { get; init; } = string.Empty;
    public string OldStatus        { get; init; } = string.Empty;
    public string NewStatus        { get; init; } = string.Empty;
    public string? FailureReason   { get; init; }
    public bool CanRetry           { get; init; }
    public DateTime ChangedAt      { get; init; }
}
