using FinancialAPI.Domain.Common;
using FinancialAPI.Domain.Entities;
using FinancialAPI.Domain.Enums;

namespace FinancialAPI.Domain.Events;

public sealed class BeneficiaryCreatedEvent : BaseDomainEvent
{
    public override string EventType => "beneficiary.created";
    public Guid BeneficiaryId { get; }
    public string OwnerId { get; }
    public string FullName { get; }
    public string AccountNumber { get; }

    public BeneficiaryCreatedEvent(Beneficiary b)
    {
        BeneficiaryId = b.Id;
        OwnerId       = b.OwnerId;
        FullName      = b.FullName;
        AccountNumber = b.AccountNumber;
    }
}

public sealed class BeneficiaryStatusChangedEvent : BaseDomainEvent
{
    public override string EventType => "beneficiary.status_changed";
    public Guid BeneficiaryId { get; }
    public BeneficiaryStatus OldStatus { get; }
    public BeneficiaryStatus NewStatus { get; }

    public BeneficiaryStatusChangedEvent(Guid id, BeneficiaryStatus oldStatus, BeneficiaryStatus newStatus)
    {
        BeneficiaryId = id;
        OldStatus     = oldStatus;
        NewStatus     = newStatus;
    }
}

public sealed class BeneficiaryUpdatedEvent : BaseDomainEvent
{
    public override string EventType => "beneficiary.updated";
    public Guid BeneficiaryId { get; }
    public BeneficiaryUpdatedEvent(Guid id) => BeneficiaryId = id;
}
