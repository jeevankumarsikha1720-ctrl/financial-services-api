using FinancialAPI.Domain.Enums;

namespace FinancialAPI.Application.DTOs.Ledger;

// ── Response ──────────────────────────────────────────────────────────────────

public class LedgerEntryResponse
{
    public Guid   Id               { get; set; }
    public string AccountId        { get; set; } = string.Empty;
    public string EntryType        { get; set; } = string.Empty;   // "Debit" | "Credit"
    public decimal Amount          { get; set; }
    public string Currency         { get; set; } = string.Empty;
    public decimal BalanceBefore   { get; set; }
    public decimal BalanceAfter    { get; set; }
    public Guid   ReferenceId      { get; set; }
    public string ReferenceType    { get; set; } = string.Empty;
    public string Description      { get; set; } = string.Empty;
    public string? ExternalReference { get; set; }
    public bool   IsReversed       { get; set; }
    public Guid?  ReversalEntryId  { get; set; }
    public string CreatedBy        { get; set; } = string.Empty;
    public DateTime CreatedAt      { get; set; }
}

public class LedgerListResponse
{
    public List<LedgerEntryResponse> Items      { get; set; } = [];
    public int  TotalCount                      { get; set; }
    public int  PageNumber                      { get; set; }
    public int  PageSize                        { get; set; }
}

public class AccountBalanceResponse
{
    public string  AccountId       { get; set; } = string.Empty;
    public decimal CurrentBalance  { get; set; }
    public string  Currency        { get; set; } = string.Empty;
    public int     EntryCount      { get; set; }
    public DateTime? LastEntryAt   { get; set; }
}

// ── Requests ─────────────────────────────────────────────────────────────────

public class PostLedgerEntryRequest
{
    public string AccountId       { get; set; } = string.Empty;
    public string EntryType       { get; set; } = string.Empty;   // "Debit" | "Credit"
    public decimal Amount         { get; set; }
    public CurrencyCode Currency  { get; set; }
    public decimal BalanceBefore  { get; set; }
    public Guid ReferenceId       { get; set; }
    public string ReferenceType   { get; set; } = string.Empty;
    public string Description     { get; set; } = string.Empty;
    public string? ExternalReference { get; set; }
}

public class ReverseLedgerEntryRequest
{
    public string Reason { get; set; } = string.Empty;
}

// ── Query ────────────────────────────────────────────────────────────────────

public class GetLedgerEntriesQuery
{
    public string? AccountId    { get; set; }
    public string? ReferenceType { get; set; }
    public Guid?   ReferenceId  { get; set; }
    public bool?   IsReversed   { get; set; }
    public DateTime? From       { get; set; }
    public DateTime? To         { get; set; }
    public int PageNumber       { get; set; } = 1;
    public int PageSize         { get; set; } = 20;
}
