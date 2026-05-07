using FinancialAPI.Domain.Common;
using FinancialAPI.Domain.Entities;
using FinancialAPI.Domain.Enums;

namespace FinancialAPI.Domain.Events;

public sealed class PaymentInitiatedEvent : BaseDomainEvent
{
    public override string EventType => "payment.initiated";
    public Guid PaymentId { get; }
    public string ReferenceNumber { get; }
    public decimal Amount { get; }
    public CurrencyCode Currency { get; }
    public PaymentType PaymentType { get; }
    public string SenderAccountId { get; }
    public Guid BeneficiaryId { get; }

    public PaymentInitiatedEvent(Payment payment)
    {
        PaymentId       = payment.Id;
        ReferenceNumber = payment.ReferenceNumber;
        Amount          = payment.Amount;
        Currency        = payment.Currency;
        PaymentType     = payment.Type;
        SenderAccountId = payment.SenderAccountId;
        BeneficiaryId   = payment.BeneficiaryId;
    }
}

public sealed class PaymentStatusChangedEvent : BaseDomainEvent
{
    public override string EventType => "payment.status_changed";
    public Guid PaymentId { get; }
    public PaymentStatus OldStatus { get; }
    public PaymentStatus NewStatus { get; }

    public PaymentStatusChangedEvent(Guid paymentId, PaymentStatus oldStatus, PaymentStatus newStatus)
    {
        PaymentId = paymentId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
    }
}

public sealed class PaymentFailedEvent : BaseDomainEvent
{
    public override string EventType => "payment.failed";
    public Guid PaymentId { get; }
    public string ReferenceNumber { get; }
    public string Reason { get; }
    public int RetryCount { get; }
    public bool CanRetry { get; }

    public PaymentFailedEvent(Payment payment)
    {
        PaymentId       = payment.Id;
        ReferenceNumber = payment.ReferenceNumber;
        Reason          = payment.FailureReason ?? "Unknown";
        RetryCount      = payment.RetryCount;
        CanRetry        = payment.CanRetry();
    }
}
