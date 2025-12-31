using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;
using Utility.Helpers.Common;

namespace DA.Persistence.Repository;

// Minimal, two-parameter repository interface for services that don't need to know the entity key type.
public interface IRepository<TEntity, TContext>
{
    //Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Delete(TEntity entity);
    //Task SaveChangesAsync();
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellation = default);
    Task<TEntity> AddAsync(TEntity entity, bool createNewId = true, CancellationToken cancellation = default);
    Task<IList<TEntity>> AddAsync(IList<TEntity> entities, bool createNewId = true, CancellationToken cancellation = default);
    Task<TEntity> AddDetachedAsync(TEntity entity, bool createNewId = true, CancellationToken cancellation = default);
    TEntity AddSync(TEntity entity, CancellationToken cancellation = default);
    Task<TEntity> UpdateAsync(TEntity entity, bool isDetached = false, CancellationToken cancellation = default);
    Task<List<TEntity>> UpdateAsync(List<TEntity> entities, bool isDetached = false, CancellationToken cancellation = default);
    Task<TEntity?> GetOneDefaultWithInclude(
       Expression<Func<TEntity, bool>> predicate, CancellationToken cancellation = default, params string[] include);
    Task<bool> Exists(object primaryKey, CancellationToken cancellation = default);
    Task<bool> Exists(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellation = default);
    Task<List<TEntity>> GetMany(Expression<Func<TEntity, bool>> where, CancellationToken cancellation = default);
    Task<(int, List<TEntity>)> GetListWithPagination(Expression<Func<TEntity, bool>> where, int PageNumber, int PageSize, int? OrderBy = 0, CancellationToken cancellation = default);
    Task<(int, List<TEntity>)> GetAllWithPagination(int PageNumber, int PageSize, int? OrderBy = 0, CancellationToken cancellation = default);
    Task<TEntity> GetFirst(Expression<Func<TEntity, bool>> predicate, int? OrderBy = 0, CancellationToken cancellation = default);
    Task<TEntity?> GetByIdAsync(object shiftId, CancellationToken cancellation = default);
    Task<(int, IList<TEntity>)> GetPaginationWithIncludeAsync(Expression<Func<TEntity, bool>> predicate, int PageNumber, int PageSize, int? OrderBy = 0, CancellationToken cancellation = default, params string[] include);
   Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellation = default);
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
        //Expression<Func<TEntity, TResult>>? selector = null,
        int pageNumber = 1,
        int pageSize = 10,
        bool includeDeleted = false,
        CancellationToken ct = default,
        params string[] include);

    //IQueryable<TEntity> GetWithInclude(
    //    Expression<Func<TEntity, bool>> predicate, 
    //    params string[] include);

    IQueryable<TEntity> GetManyIQueryable(Expression<Func<TEntity, bool>> where);
    IQueryable<TEntity> GetManyIQueryableWithDeleted(Expression<Func<TEntity, bool>> where);
    (IQueryable<TEntity>, int) GetWithIncludePaginatedQueryAble(Expression<Func<TEntity, bool>> predicate, int PageNumber, int PageSize, params string[] include);
    (IQueryable<TEntity>, int) GetWithIncludeQueryAbleWithoutOrder(Expression<Func<TEntity, bool>> predicate, params string[] include);
    (IQueryable<TEntity>, int) GetPaginatedQueryAble(Expression<Func<TEntity, bool>> predicate, int PageNumber, int PageSize);
    Task<TEntity?> GetFirstOrDefaultWithInclude(
        Expression<Func<TEntity, bool>> predicate,
        bool includeDeleted = false,
        CancellationToken ct = default,
        params string[] include);

    Task<List<TResult>> GetMany<TResult>(
        Expression<Func<TEntity, bool>> where,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellation = default);

    Task<TResult?> GetFirstOrDefaultWithInclude<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>> selector,
        bool includeDeleted = false,
        CancellationToken ct = default,
        params string[] include);

    Task<TEntity?> GetLastOrDefaultWithInclude(
        Expression<Func<TEntity, bool>> predicate,
        bool includeDeleted = false,
        CancellationToken ct = default,
        params string[] include);

    Task<TResult?> GetLastOrDefaultWithInclude<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>> selector,
        bool includeDeleted = false,
        CancellationToken ct = default,
        params string[] include);
    //Task<int> ExecuteUpdateAsync(Expression<Func<TEntity, bool>> filter, Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>> setPropertyCalls);
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
    Task<List<TEntity>> DeleteAsync(List<TEntity> entities, bool isDetached = false, CancellationToken cancellation = default);
    Task<bool> DeleteWithIdsAsync(List<Guid> entitieIds, CancellationToken cancellation = default);
}
