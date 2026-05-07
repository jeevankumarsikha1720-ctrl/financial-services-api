using FinancialAPI.Domain.Common;
using FinancialAPI.Domain.Entities;
using FinancialAPI.Domain.Enums;

namespace FinancialAPI.Domain.Events;

public sealed class LedgerEntryPostedEvent : BaseDomainEvent
{
    public override string EventType => "ledger.entry_posted";
    public Guid EntryId { get; }
    public string AccountId { get; }
    public LedgerEntryType EntryType { get; }
    public decimal Amount { get; }
    public CurrencyCode Currency { get; }
    public decimal BalanceBefore { get; }
    public decimal BalanceAfter { get; }
    public Guid ReferenceId { get; }

    public LedgerEntryPostedEvent(LedgerEntry entry)
    {
        EntryId       = entry.Id;
        AccountId     = entry.AccountId;
        EntryType     = entry.EntryType;
        Amount        = entry.Amount;
        Currency      = entry.Currency;
        BalanceBefore = entry.BalanceBefore;
        BalanceAfter  = entry.BalanceAfter;
        ReferenceId   = entry.ReferenceId;
    }
}
