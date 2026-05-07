using FinancialAPI.Application.DTOs.Ledger;

namespace FinancialAPI.Application.Interfaces;

public interface ILedgerService
{
    /// <summary>Post a debit or credit entry to the ledger.</summary>
    Task<LedgerEntryResponse> PostEntryAsync(
        PostLedgerEntryRequest request, string createdBy, CancellationToken ct = default);

    /// <summary>Get a single ledger entry by ID.</summary>
    Task<LedgerEntryResponse> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Get paginated ledger entries with optional filtering.</summary>
    Task<LedgerListResponse> GetAllAsync(GetLedgerEntriesQuery query, CancellationToken ct = default);

    /// <summary>Get the latest balance for an account (last BalanceAfter value).</summary>
    Task<AccountBalanceResponse> GetBalanceAsync(string accountId, CancellationToken ct = default);

    /// <summary>Reverse an existing ledger entry (creates an offsetting entry).</summary>
    Task<LedgerEntryResponse> ReverseAsync(Guid id, string reversedBy, CancellationToken ct = default);
}
