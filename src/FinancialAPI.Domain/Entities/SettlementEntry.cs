using FinancialAPI.Domain.Common;

namespace FinancialAPI.Domain.Entities;

public class SettlementEntry : BaseEntity
{
    public Guid SettlementId { get; private set; }
    public Guid PaymentId { get; private set; }
    public decimal GrossAmount { get; private set; }
    public decimal Fee { get; private set; }
    public decimal NetAmount => GrossAmount - Fee;
    public bool IsFailed { get; private set; } = false;
    public string? FailureReason { get; private set; }

    private SettlementEntry() { }

    public static SettlementEntry Create(Guid settlementId, Guid paymentId, decimal grossAmount, decimal fee)
    {
        return new SettlementEntry
        {
            SettlementId = settlementId,
            PaymentId    = paymentId,
            GrossAmount  = grossAmount,
            Fee          = fee
        };
    }

    public void MarkAsFailed(string reason)
    {
        IsFailed      = true;
        FailureReason = reason;
        SetUpdatedAt();
    }
}
