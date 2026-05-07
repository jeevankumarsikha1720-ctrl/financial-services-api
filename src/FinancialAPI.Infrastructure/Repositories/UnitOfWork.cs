using FinancialAPI.Application.Interfaces;
using FinancialAPI.Domain.Entities;
using FinancialAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace FinancialAPI.Infrastructure.Repositories;

/// <summary>
/// Unit of Work implementation.
/// Creates repositories lazily (only when first accessed).
/// All repositories share the same DbContext instance = same transaction.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly FinancialDbContext _context;
    private IDbContextTransaction? _transaction;

    // Lazily initialised repositories
    private IRepository<Payment>?         _payments;
    private IRepository<Settlement>?      _settlements;
    private IRepository<SettlementEntry>? _settlementEntries;
    private IRepository<Beneficiary>?     _beneficiaries;
    private IRepository<LedgerEntry>?     _ledgerEntries;
    private IRepository<FraudAlert>?      _fraudAlerts;
    private IRepository<Notification>?    _notifications;

    public UnitOfWork(FinancialDbContext context)
        => _context = context;

    public IRepository<Payment>        Payments          => _payments         ??= new Repository<Payment>(_context);
    public IRepository<Settlement>     Settlements       => _settlements      ??= new Repository<Settlement>(_context);
    public IRepository<SettlementEntry> SettlementEntries => _settlementEntries ??= new Repository<SettlementEntry>(_context);
    public IRepository<Beneficiary>    Beneficiaries     => _beneficiaries    ??= new Repository<Beneficiary>(_context);
    public IRepository<LedgerEntry>    LedgerEntries     => _ledgerEntries    ??= new Repository<LedgerEntry>(_context);
    public IRepository<FraudAlert>     FraudAlerts       => _fraudAlerts      ??= new Repository<FraudAlert>(_context);
    public IRepository<Notification>   Notifications     => _notifications    ??= new Repository<Notification>(_context);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default)
        => _transaction = await _context.Database.BeginTransactionAsync(ct);

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("No transaction in progress.");
        await _transaction.CommitAsync(ct);
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("No transaction in progress.");
        await _transaction.RollbackAsync(ct);
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
