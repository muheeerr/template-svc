using DA.Specifications;
using System.Linq.Expressions;
using DA.Common;

namespace DA.Persistence.Repository;

/// <summary>
/// Specification-first repository. Prefer spec queries for complex scenarios;
/// use the convenience overloads (GetWithInclude, GetFirstOrDefault, etc.) for simpler cases.
/// </summary>
public interface IRepository<TEntity, TContext>
{
    // ── Spec-based queries ──────────────────────────────────────────────
    Task<TEntity?> GetBySpecAsync(BaseSpecification<TEntity> spec, CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> ListBySpecAsync(BaseSpecification<TEntity> spec, CancellationToken ct = default);
    Task<int> CountBySpecAsync(BaseSpecification<TEntity> spec, CancellationToken ct = default);
    Task<bool> ExistsBySpecAsync(BaseSpecification<TEntity> spec, CancellationToken ct = default);

    // ── Convenience queries ─────────────────────────────────────────────
    Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellation = default);
    Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellation = default);
    Task<bool> Exists(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellation = default);

    Task<PagedResult<TResult>> GetWithInclude<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>>? selector = null,
        int pageNumber = 1,
        int pageSize = 10,
        bool includeDeleted = false,
        CancellationToken ct = default,
        params string[] include);

    Task<PagedResult<TEntity>> GetWithInclude(
        Expression<Func<TEntity, bool>> predicate,
        int pageNumber = 1,
        int pageSize = 10,
        bool includeDeleted = false,
        CancellationToken ct = default,
        params string[] include);

    Task<TEntity?> GetFirstOrDefaultWithInclude(
        Expression<Func<TEntity, bool>> predicate,
        bool includeDeleted = false,
        CancellationToken ct = default,
        params string[] include);

    Task<TResult?> GetFirstOrDefaultWithInclude<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>> selector,
        bool includeDeleted = false,
        CancellationToken ct = default,
        params string[] include);

    Task<PagedResult<TResult>> GetManyPaginated<TResult>(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellation = default);

    Task<Dictionary<TKey, TEntity>> ToDictionaryAsync<TKey>(
        Expression<Func<TEntity, bool>> predicate,
        Func<TEntity, TKey> keySelector,
        CancellationToken cancellation = default) where TKey : notnull;

    Task<Dictionary<TKey, TValue>> ToDictionaryAsync<TKey, TValue>(
        Expression<Func<TEntity, bool>> predicate,
        Func<TEntity, TKey> keySelector,
        Func<TEntity, TValue> valueSelector,
        CancellationToken cancellation = default) where TKey : notnull;

    // ── Mutations ───────────────────────────────────────────────────────
    Task<TEntity> AddAsync(TEntity entity, bool createNewId = true, CancellationToken cancellation = default);
    Task<IList<TEntity>> AddRangeAsync(IList<TEntity> entities, bool createNewId = true, CancellationToken cancellation = default);
    void Update(TEntity entity);
    void Delete(TEntity entity);
    Task<List<TEntity>> DeleteAsync(List<TEntity> entities, bool isDetached = false, CancellationToken cancellation = default);
    Task<bool> DeleteWithIdsAsync(List<Guid> entityIds, CancellationToken cancellation = default);
}
