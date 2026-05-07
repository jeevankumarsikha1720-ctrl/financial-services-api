using System.Linq.Expressions;
using FinancialAPI.Application.Interfaces;
using FinancialAPI.Domain.Common;
using FinancialAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinancialAPI.Infrastructure.Repositories;

/// <summary>
/// Generic EF Core repository implementation.
/// All domain-specific repositories reuse this — no code duplication.
/// </summary>
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly FinancialDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(FinancialDbContext context)
    {
        _context = context;
        _dbSet   = context.Set<T>();
    }

    // ── Queries ───────────────────────────────────────────────

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        => await _dbSet.AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default)
        => await _dbSet.AsNoTracking().Where(predicate).ToListAsync(ct);

    public async Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default)
        => await _dbSet.AsNoTracking().FirstOrDefaultAsync(predicate, ct);

    public async Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default)
        => await _dbSet.AnyAsync(predicate, ct);

    public async Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken ct = default)
        => predicate is null
            ? await _dbSet.CountAsync(ct)
            : await _dbSet.CountAsync(predicate, ct);

    // ── Commands ──────────────────────────────────────────────

    public async Task AddAsync(T entity, CancellationToken ct = default)
        => await _dbSet.AddAsync(entity, ct);

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
        => await _dbSet.AddRangeAsync(entities, ct);

    public void Update(T entity)
        => _dbSet.Update(entity);

    public void Delete(T entity)
    {
        // Always soft-delete instead of hard-delete
        entity.SoftDelete();
        _dbSet.Update(entity);
    }

    // ── Pagination ────────────────────────────────────────────

    public async Task<(IReadOnlyList<T> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, object>>? orderBy = null,
        bool descending = true,
        CancellationToken ct = default)
    {
        var query = _dbSet.AsNoTracking();

        if (predicate is not null)
            query = query.Where(predicate);

        var totalCount = await query.CountAsync(ct);

        if (orderBy is not null)
            query = descending
                ? query.OrderByDescending(orderBy)
                : query.OrderBy(orderBy);
        else
            query = query.OrderByDescending(e => e.CreatedAt); // default sort

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
