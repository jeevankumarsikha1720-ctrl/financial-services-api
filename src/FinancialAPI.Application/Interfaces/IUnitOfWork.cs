using FinancialAPI.Domain.Entities;

namespace FinancialAPI.Application.Interfaces;

/// <summary>
/// Unit of Work — wraps all repositories under a single database transaction.
/// Call SaveChangesAsync() once to commit everything atomically.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IRepository<Payment>        Payments        { get; }
    IRepository<Settlement>     Settlements     { get; }
    IRepository<SettlementEntry> SettlementEntries { get; }
    IRepository<Beneficiary>    Beneficiaries   { get; }
    IRepository<LedgerEntry>    LedgerEntries   { get; }
    IRepository<FraudAlert>     FraudAlerts     { get; }
    IRepository<Notification>   Notifications   { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
