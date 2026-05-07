using System.Linq.Expressions;
using FinancialAPI.Domain.Common;

namespace FinancialAPI.Application.Interfaces;

/// <summary>
/// Generic async repository interface used by all application services.
/// Keeps services decoupled from EF Core.
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    // ── Queries ───────────────────────────────────────────────
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);

    Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default);

    Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default);

    Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default);

    Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken ct = default);

    // ── Commands ──────────────────────────────────────────────
    Task AddAsync(T entity, CancellationToken ct = default);

    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);

    void Update(T entity);

    void Delete(T entity);

    // ── Pagination ────────────────────────────────────────────
    Task<(IReadOnlyList<T> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, object>>? orderBy = null,
        bool descending = true,
        CancellationToken ct = default);
}
