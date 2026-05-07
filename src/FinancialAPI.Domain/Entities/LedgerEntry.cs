using FinancialAPI.Domain.Common;
using FinancialAPI.Domain.Enums;
using FinancialAPI.Domain.Events;

namespace FinancialAPI.Domain.Entities;

public class LedgerEntry : BaseEntity
{
    public string AccountId { get; private set; } = string.Empty;
    public LedgerEntryType EntryType { get; private set; }
    public decimal Amount { get; private set; }
    public CurrencyCode Currency { get; private set; }
    public decimal BalanceBefore { get; private set; }
    public decimal BalanceAfter { get; private set; }
    public Guid ReferenceId { get; private set; }
    public string ReferenceType { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? ExternalReference { get; private set; }
    public bool IsReversed { get; private set; } = false;
    public Guid? ReversalEntryId { get; private set; }

    private LedgerEntry() { }

    public static LedgerEntry CreateDebit(
        string accountId, decimal amount, CurrencyCode currency,
        decimal balanceBefore, Guid referenceId, string referenceType,
        string description, string createdBy)
        => Create(accountId, LedgerEntryType.Debit, amount, currency,
                  balanceBefore, balanceBefore - amount,
                  referenceId, referenceType, description, createdBy);

    public static LedgerEntry CreateCredit(
        string accountId, decimal amount, CurrencyCode currency,
        decimal balanceBefore, Guid referenceId, string referenceType,
        string description, string createdBy)
        => Create(accountId, LedgerEntryType.Credit, amount, currency,
                  balanceBefore, balanceBefore + amount,
                  referenceId, referenceType, description, createdBy);

    private static LedgerEntry Create(
        string accountId, LedgerEntryType entryType, decimal amount,
        CurrencyCode currency, decimal balanceBefore, decimal balanceAfter,
        Guid referenceId, string referenceType, string description, string createdBy)
    {
        if (amount <= 0)
            throw new ArgumentException("Ledger entry amount must be positive.", nameof(amount));

        var entry = new LedgerEntry
        {
            AccountId     = accountId,
            EntryType     = entryType,
            Amount        = amount,
            Currency      = currency,
            BalanceBefore = balanceBefore,
            BalanceAfter  = balanceAfter,
            ReferenceId   = referenceId,
            ReferenceType = referenceType,
            Description   = description,
            CreatedBy     = createdBy
        };
        entry.AddDomainEvent(new LedgerEntryPostedEvent(entry));
        return entry;
    }

    public void Reverse(Guid reversalEntryId)
    {
        if (IsReversed) throw new InvalidOperationException("Already reversed.");
        IsReversed      = true;
        ReversalEntryId = reversalEntryId;
        SetUpdatedAt();
    }
}
