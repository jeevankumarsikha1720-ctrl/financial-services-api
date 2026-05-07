using FinancialAPI.Domain.Common;
using FinancialAPI.Domain.Enums;
using FinancialAPI.Domain.Events;

namespace FinancialAPI.Domain.Entities;

public class Payment : BaseEntity
{
    public string ReferenceNumber { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public CurrencyCode Currency { get; private set; }
    public PaymentType Type { get; private set; }
    public PaymentStatus Status { get; private set; }

    public string SenderAccountId { get; private set; } = string.Empty;
    public string SenderName { get; private set; } = string.Empty;
    public Guid BeneficiaryId { get; private set; }
    public string BeneficiaryAccountNumber { get; private set; } = string.Empty;
    public string BeneficiaryBankCode { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;
    public string? FailureReason { get; private set; }
    public int RetryCount { get; private set; } = 0;
    public int MaxRetries { get; private set; } = 3;
    public DateTime? ProcessedAt { get; private set; }
    public DateTime? SettledAt { get; private set; }

    public double FraudScore { get; private set; } = 0.0;
    public bool IsFlagged { get; private set; } = false;

    private Payment() { }

    public static Payment Create(
        string senderAccountId,
        string senderName,
        Guid beneficiaryId,
        string beneficiaryAccountNumber,
        string beneficiaryBankCode,
        decimal amount,
        CurrencyCode currency,
        PaymentType type,
        string description,
        string createdBy)
    {
        if (amount <= 0)
            throw new ArgumentException("Payment amount must be greater than zero.", nameof(amount));

        var payment = new Payment
        {
            ReferenceNumber          = GenerateReferenceNumber(),
            Amount                   = amount,
            Currency                 = currency,
            Type                     = type,
            Status                   = PaymentStatus.Pending,
            SenderAccountId          = senderAccountId,
            SenderName               = senderName,
            BeneficiaryId            = beneficiaryId,
            BeneficiaryAccountNumber = beneficiaryAccountNumber,
            BeneficiaryBankCode      = beneficiaryBankCode,
            Description              = description,
            CreatedBy                = createdBy
        };

        payment.AddDomainEvent(new PaymentInitiatedEvent(payment));
        return payment;
    }

    public void MarkAsProcessing()
    {
        if (Status != PaymentStatus.Pending && Status != PaymentStatus.Failed)
            throw new InvalidOperationException($"Cannot process payment in '{Status}' status.");

        Status      = PaymentStatus.Processing;
        ProcessedAt = DateTime.UtcNow;
        SetUpdatedAt();
        AddDomainEvent(new PaymentStatusChangedEvent(Id, PaymentStatus.Pending, PaymentStatus.Processing));
    }

    public void MarkAsSettled()
    {
        if (Status != PaymentStatus.Processing)
            throw new InvalidOperationException($"Cannot settle payment in '{Status}' status.");

        Status     = PaymentStatus.Settled;
        SettledAt  = DateTime.UtcNow;
        SetUpdatedAt();
        AddDomainEvent(new PaymentStatusChangedEvent(Id, PaymentStatus.Processing, PaymentStatus.Settled));
    }

    public void MarkAsFailed(string reason)
    {
        if (Status == PaymentStatus.Settled || Status == PaymentStatus.Cancelled)
            throw new InvalidOperationException($"Cannot fail payment in '{Status}' status.");

        Status        = PaymentStatus.Failed;
        FailureReason = reason;
        SetUpdatedAt();
        AddDomainEvent(new PaymentStatusChangedEvent(Id, Status, PaymentStatus.Failed));
    }

    public void Cancel(string reason)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException("Only pending payments can be cancelled.");

        Status        = PaymentStatus.Cancelled;
        FailureReason = reason;
        SetUpdatedAt();
    }

    public void PlaceOnHold()
    {
        Status    = PaymentStatus.OnHold;
        IsFlagged = true;
        SetUpdatedAt();
    }

    public bool CanRetry() => Status == PaymentStatus.Failed && RetryCount < MaxRetries;

    public void IncrementRetry()
    {
        if (!CanRetry())
            throw new InvalidOperationException("Payment cannot be retried.");

        RetryCount++;
        Status        = PaymentStatus.Pending;
        FailureReason = null;
        SetUpdatedAt();
    }

    public void SetFraudScore(double score)
    {
        FraudScore = score;
        if (score >= 0.8) PlaceOnHold();
        SetUpdatedAt();
    }

    private static string GenerateReferenceNumber()
        => $"PAY-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
}
