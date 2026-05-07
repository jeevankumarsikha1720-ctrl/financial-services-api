using FinancialAPI.Domain.Common;
using FinancialAPI.Domain.Entities;
using FinancialAPI.Domain.Enums;

namespace FinancialAPI.Domain.Events;

public sealed class SettlementCreatedEvent : BaseDomainEvent
{
    public override string EventType => "settlement.created";
    public Guid SettlementId { get; }
    public string BatchReference { get; }
    public CurrencyCode Currency { get; }
    public DateTime SettlementDate { get; }

    public SettlementCreatedEvent(Settlement settlement)
    {
        SettlementId   = settlement.Id;
        BatchReference = settlement.BatchReference;
        Currency       = settlement.Currency;
        SettlementDate = settlement.SettlementDate;
    }
}

public sealed class SettlementStatusChangedEvent : BaseDomainEvent
{
    public override string EventType => "settlement.status_changed";
    public Guid SettlementId { get; }
    public SettlementStatus OldStatus { get; }
    public SettlementStatus NewStatus { get; }

    public SettlementStatusChangedEvent(Guid settlementId, SettlementStatus oldStatus, SettlementStatus newStatus)
    {
        SettlementId = settlementId;
        OldStatus    = oldStatus;
        NewStatus    = newStatus;
    }
}
