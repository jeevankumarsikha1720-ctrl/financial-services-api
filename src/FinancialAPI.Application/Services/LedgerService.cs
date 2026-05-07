using FinancialAPI.Application.DTOs.Ledger;
using FinancialAPI.Application.Interfaces;
using FinancialAPI.Domain.Entities;
using FinancialAPI.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace FinancialAPI.Application.Services;

public class LedgerService : ILedgerService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<LedgerService> _logger;

    public LedgerService(IUnitOfWork uow, ILogger<LedgerService> logger)
    {
        _uow    = uow;
        _logger = logger;
    }

    // ── Post Entry ────────────────────────────────────────────────────────────

    public async Task<LedgerEntryResponse> PostEntryAsync(
        PostLedgerEntryRequest request, string createdBy, CancellationToken ct = default)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be positive.");

        LedgerEntry entry;

        if (string.Equals(request.EntryType, "Debit", StringComparison.OrdinalIgnoreCase))
        {
            entry = LedgerEntry.CreateDebit(
                request.AccountId, request.Amount, request.Currency,
                request.BalanceBefore, request.ReferenceId, request.ReferenceType,
                request.Description, createdBy);
        }
        else if (string.Equals(request.EntryType, "Credit", StringComparison.OrdinalIgnoreCase))
        {
            entry = LedgerEntry.CreateCredit(
                request.AccountId, request.Amount, request.Currency,
                request.BalanceBefore, request.ReferenceId, request.ReferenceType,
                request.Description, createdBy);
        }
        else
        {
            throw new ArgumentException($"Unknown EntryType '{request.EntryType}'. Use 'Debit' or 'Credit'.");
        }

        await _uow.LedgerEntries.AddAsync(entry, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Ledger entry posted. Id: {Id}, Account: {Account}, Type: {Type}, Amount: {Amount}",
            entry.Id, entry.AccountId, entry.EntryType, entry.Amount);

        return MapToResponse(entry);
    }

    // ── Get by ID ────────────────────────────────────────────────────────────

    public async Task<LedgerEntryResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entry = await _uow.LedgerEntries.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Ledger entry {id} not found.");
        return MapToResponse(entry);
    }

    // ── Get All (paged) ───────────────────────────────────────────────────────

    public async Task<LedgerListResponse> GetAllAsync(
        GetLedgerEntriesQuery query, CancellationToken ct = default)
    {
        var (items, total) = await _uow.LedgerEntries.GetPagedAsync(
            pageNumber: query.PageNumber,
            pageSize:   query.PageSize,
            predicate:  e =>
                (query.AccountId     == null || e.AccountId     == query.AccountId)     &&
                (query.ReferenceType == null || e.ReferenceType == query.ReferenceType) &&
                (query.ReferenceId   == null || e.ReferenceId   == query.ReferenceId)   &&
                (query.IsReversed    == null || e.IsReversed     == query.IsReversed)    &&
                (query.From          == null || e.CreatedAt      >= query.From)          &&
                (query.To            == null || e.CreatedAt      <= query.To),
            orderBy:    e => e.CreatedAt,
            descending: true,
            ct:         ct);

        return new LedgerListResponse
        {
            Items      = items.Select(MapToResponse).ToList(),
            TotalCount = total,
            PageNumber = query.PageNumber,
            PageSize   = query.PageSize
        };
    }

    // ── Balance ───────────────────────────────────────────────────────────────

    public async Task<AccountBalanceResponse> GetBalanceAsync(
        string accountId, CancellationToken ct = default)
    {
        // Get all entries for this account, newest first
        var (entries, total) = await _uow.LedgerEntries.GetPagedAsync(
            pageNumber: 1,
            pageSize:   1,
            predicate:  e => e.AccountId == accountId,
            orderBy:    e => e.CreatedAt,
            descending: true,
            ct:         ct);

        var latest = entries.FirstOrDefault();

        return new AccountBalanceResponse
        {
            AccountId      = accountId,
            CurrentBalance = latest?.BalanceAfter ?? 0m,
            Currency       = latest?.Currency.ToString() ?? string.Empty,
            EntryCount     = total,
            LastEntryAt    = latest?.CreatedAt
        };
    }

    // ── Reverse ───────────────────────────────────────────────────────────────

    public async Task<LedgerEntryResponse> ReverseAsync(
        Guid id, string reversedBy, CancellationToken ct = default)
    {
        var original = await _uow.LedgerEntries.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Ledger entry {id} not found.");

        if (original.IsReversed)
            throw new InvalidOperationException("This ledger entry has already been reversed.");

        // Create the offsetting entry (Debit ↔ Credit)
        LedgerEntry reversal;
        if (original.EntryType == LedgerEntryType.Debit)
        {
            reversal = LedgerEntry.CreateCredit(
                original.AccountId, original.Amount, original.Currency,
                original.BalanceAfter,  // reverse brings balance back up
                original.Id, "Reversal",
                $"Reversal of entry {original.Id}", reversedBy);
        }
        else
        {
            reversal = LedgerEntry.CreateDebit(
                original.AccountId, original.Amount, original.Currency,
                original.BalanceAfter,  // reverse brings balance back down
                original.Id, "Reversal",
                $"Reversal of entry {original.Id}", reversedBy);
        }

        await _uow.LedgerEntries.AddAsync(reversal, ct);

        // Mark original as reversed
        original.Reverse(reversal.Id);
        _uow.LedgerEntries.Update(original);

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Ledger entry reversed. OriginalId: {OrigId}, ReversalId: {RevId}",
            id, reversal.Id);

        return MapToResponse(reversal);
    }

    // ── Mapper ────────────────────────────────────────────────────────────────

    private static LedgerEntryResponse MapToResponse(LedgerEntry e) => new()
    {
        Id                = e.Id,
        AccountId         = e.AccountId,
        EntryType         = e.EntryType.ToString(),
        Amount            = e.Amount,
        Currency          = e.Currency.ToString(),
        BalanceBefore     = e.BalanceBefore,
        BalanceAfter      = e.BalanceAfter,
        ReferenceId       = e.ReferenceId,
        ReferenceType     = e.ReferenceType,
        Description       = e.Description,
        ExternalReference = e.ExternalReference,
        IsReversed        = e.IsReversed,
        ReversalEntryId   = e.ReversalEntryId,
        CreatedBy         = e.CreatedBy,
        CreatedAt         = e.CreatedAt
    };
}
