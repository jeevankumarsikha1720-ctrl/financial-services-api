using FinancialAPI.Domain.Common;
using FinancialAPI.Domain.Enums;
using FinancialAPI.Domain.Events;

namespace FinancialAPI.Domain.Entities;

public class Settlement : BaseEntity
{
    public string BatchReference { get; private set; } = string.Empty;
    public SettlementStatus Status { get; private set; }
    public CurrencyCode Currency { get; private set; }
    public DateTime SettlementDate { get; private set; }

    public decimal TotalGrossAmount { get; private set; } = 0m;
    public decimal TotalFees { get; private set; } = 0m;
    public decimal NetSettlementAmount { get; private set; } = 0m;
    public int TotalTransactionCount { get; private set; } = 0;
    public int SuccessfulCount { get; private set; } = 0;
    public int FailedCount { get; private set; } = 0;

    public string? ReconciliationReference { get; private set; }
    public DateTime? ReconciledAt { get; private set; }
    public string? Notes { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private readonly List<SettlementEntry> _entries = new();
    public IReadOnlyCollection<SettlementEntry> Entries => _entries.AsReadOnly();

    private Settlement() { }

    public static Settlement Create(CurrencyCode currency, DateTime settlementDate, string createdBy)
    {
        var settlement = new Settlement
        {
            BatchReference = GenerateBatchReference(),
            Status         = SettlementStatus.Draft,
            Currency       = currency,
            SettlementDate = settlementDate,
            CreatedBy      = createdBy
        };
        settlement.AddDomainEvent(new SettlementCreatedEvent(settlement));
        return settlement;
    }

    public void AddEntry(Guid paymentId, decimal grossAmount, decimal fee)
    {
        if (Status != SettlementStatus.Draft)
            throw new InvalidOperationException("Entries can only be added to Draft settlements.");

        _entries.Add(SettlementEntry.Create(Id, paymentId, grossAmount, fee));
        TotalGrossAmount += grossAmount;
        TotalFees        += fee;
        TotalTransactionCount++;
        SetUpdatedAt();
    }

    public void StartProcessing()
    {
        if (Status != SettlementStatus.Draft)
            throw new InvalidOperationException("Only Draft settlements can be started.");
        if (!_entries.Any())
            throw new InvalidOperationException("Cannot process an empty settlement batch.");

        Status = SettlementStatus.InProgress;
        NetSettlementAmount = TotalGrossAmount - TotalFees;
        SetUpdatedAt();
        AddDomainEvent(new SettlementStatusChangedEvent(Id, SettlementStatus.Draft, SettlementStatus.InProgress));
    }

    public void Complete(string reconciliationRef)
    {
        if (Status != SettlementStatus.InProgress)
            throw new InvalidOperationException("Only InProgress settlements can be completed.");

        Status                  = SettlementStatus.Completed;
        ReconciliationReference = reconciliationRef;
        CompletedAt             = DateTime.UtcNow;
        SuccessfulCount         = _entries.Count(e => !e.IsFailed);
        FailedCount             = _entries.Count(e => e.IsFailed);
        SetUpdatedAt();
        AddDomainEvent(new SettlementStatusChangedEvent(Id, SettlementStatus.InProgress, SettlementStatus.Completed));
    }

    public void Reconcile(string notes)
    {
        if (Status != SettlementStatus.Completed)
            throw new InvalidOperationException("Only Completed settlements can be reconciled.");

        Status        = SettlementStatus.Reconciled;
        ReconciledAt  = DateTime.UtcNow;
        Notes         = notes;
        SetUpdatedAt();
    }

    public void MarkAsFailed(string reason)
    {
        Status        = SettlementStatus.Failed;
        FailureReason = reason;
        SetUpdatedAt();
        AddDomainEvent(new SettlementStatusChangedEvent(Id, Status, SettlementStatus.Failed));
    }

    private static string GenerateBatchReference()
        => $"SETL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
}
