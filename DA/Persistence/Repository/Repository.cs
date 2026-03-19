using DA.Common;
using DA.Entities;
using DA.Specifications;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace DA.Persistence.Repository;

public class Repository<TEntity, TContext> : IRepository<TEntity, TContext>
    where TEntity : Entity
    where TContext : BaseContext
{
    private const int MaxPageSize = 100;
    private readonly TContext _context;
    private readonly DbSet<TEntity> _dbSet;

    public Repository(TContext context)
    {
        _context = context;
        _dbSet = _context.Set<TEntity>();
    }

    // ── Spec-based queries ──────────────────────────────────────────────

    public async Task<TEntity?> GetBySpecAsync(BaseSpecification<TEntity> spec, CancellationToken ct = default)
        => await SpecificationEvaluator<TEntity>.GetQuery(_dbSet.AsQueryable(), spec).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<TEntity>> ListBySpecAsync(BaseSpecification<TEntity> spec, CancellationToken ct = default)
        => await SpecificationEvaluator<TEntity>.GetQuery(_dbSet.AsQueryable(), spec).ToListAsync(ct);

    public async Task<int> CountBySpecAsync(BaseSpecification<TEntity> spec, CancellationToken ct = default)
        => await SpecificationEvaluator<TEntity>.GetQuery(_dbSet.AsQueryable(), spec).CountAsync(ct);

    public async Task<bool> ExistsBySpecAsync(BaseSpecification<TEntity> spec, CancellationToken ct = default)
        => await SpecificationEvaluator<TEntity>.GetQuery(_dbSet.AsQueryable(), spec).AnyAsync(ct);

    // ── Convenience queries ─────────────────────────────────────────────

    public async Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellation = default)
    {
        return await _dbSet.FindAsync(id, cancellation);
    }

    public async Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellation = default)
    {
        return await _dbSet.CountAsync(predicate, cancellation);
    }

    public async Task<bool> Exists(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellation = default)
    {
        return await _dbSet.Where(x => !x.IsDeleted).AnyAsync(predicate, cancellation);
    }

    public async Task<PagedResult<TResult>> GetWithInclude<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>>? selector = null,
        int pageNumber = 1,
        int pageSize = 10,
        bool includeDeleted = false,
        CancellationToken ct = default,
        params string[] include)
    {
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        include ??= [];
        IQueryable<TEntity> query = _dbSet.AsNoTracking();
        query = include.Aggregate(query, (current, inc) => current.Include(inc));

        query = includeDeleted
            ? query.IgnoreQueryFilters().Where(predicate)
            : query.Where(predicate).Where(x => !x.IsDeleted);

        int totalCount = await query.CountAsync(ct);
        query = query.OrderByDescending(x => x.CreatedAt);

        IQueryable<TResult> projectedQuery;
        if (selector is null)
        {
            if (typeof(TResult) == typeof(TEntity))
                projectedQuery = (IQueryable<TResult>)query;
            else
                throw new InvalidOperationException(
                    $"Selector cannot be null when TResult ({typeof(TResult).Name}) differs from TEntity ({typeof(TEntity).Name}).");
        }
        else
        {
            projectedQuery = query.Select(selector);
        }

        var items = await projectedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<TResult>
        {
            Items = items,
            PaginationData = new PaginationData
            {
                TotalRecords = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            }
        };
    }

    public async Task<PagedResult<TEntity>> GetWithInclude(
        Expression<Func<TEntity, bool>> predicate,
        int pageNumber = 1,
        int pageSize = 10,
        bool includeDeleted = false,
        CancellationToken ct = default,
        params string[] include)
    {
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        include ??= [];
        IQueryable<TEntity> query = _dbSet.AsNoTracking();
        query = include.Aggregate(query, (current, inc) => current.Include(inc));

        query = includeDeleted
            ? query.IgnoreQueryFilters().Where(predicate)
            : query.Where(predicate).Where(x => !x.IsDeleted);

        int totalCount = await query.CountAsync(ct);
        query = query.OrderByDescending(x => x.CreatedAt);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<TEntity>
        {
            Items = items,
            PaginationData = new PaginationData
            {
                TotalRecords = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            }
        };
    }

    public async Task<TEntity?> GetFirstOrDefaultWithInclude(
        Expression<Func<TEntity, bool>> predicate,
        bool includeDeleted = false,
        CancellationToken ct = default,
        params string[] include)
    {
        IQueryable<TEntity> query = _dbSet.AsNoTracking();
        query = include.Aggregate(query, (current, inc) => current.Include(inc));
        if (!includeDeleted)
            query = query.Where(x => !x.IsDeleted);
        return await query.Where(predicate).OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(ct);
    }

    public async Task<TResult?> GetFirstOrDefaultWithInclude<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>> selector,
        bool includeDeleted = false,
        CancellationToken ct = default,
        params string[] include)
    {
        IQueryable<TEntity> query = _dbSet;
        query = include.Aggregate(query, (current, inc) => current.Include(inc));
        if (!includeDeleted)
            query = query.Where(x => !x.IsDeleted);
        return await query.Where(predicate).OrderBy(x => x.CreatedAt).Select(selector).FirstOrDefaultAsync(ct);
    }

    public async Task<PagedResult<TResult>> GetManyPaginated<TResult>(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellation = default)
    {
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var query = _dbSet.AsNoTracking();
        if (predicate != null)
            query = query.Where(predicate);
        query = query.Where(x => !x.IsDeleted);
        int totalCount = await query.CountAsync(cancellation);
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Select(selector)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellation);
        return new PagedResult<TResult>
        {
            Items = items,
            PaginationData = new PaginationData
            {
                TotalRecords = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            }
        };
    }

    public async Task<Dictionary<TKey, TEntity>> ToDictionaryAsync<TKey>(
        Expression<Func<TEntity, bool>> predicate,
        Func<TEntity, TKey> keySelector,
        CancellationToken cancellation = default) where TKey : notnull
    {
        var items = await _dbSet.AsNoTracking()
            .Where(predicate)
            .Where(x => !x.IsDeleted)
            .ToListAsync(cancellation);

        return items.ToDictionary(keySelector);
    }

    public async Task<Dictionary<TKey, TValue>> ToDictionaryAsync<TKey, TValue>(
        Expression<Func<TEntity, bool>> predicate,
        Func<TEntity, TKey> keySelector,
        Func<TEntity, TValue> valueSelector,
        CancellationToken cancellation = default) where TKey : notnull
    {
        var items = await _dbSet.AsNoTracking().IgnoreQueryFilters()
            .Where(predicate)
            .ToListAsync(cancellation);

        return items.ToDictionary(keySelector, valueSelector);
    }

    // ── Mutations ───────────────────────────────────────────────────────

    public async Task<TEntity> AddAsync(TEntity entity, bool createNewId = true, CancellationToken cancellation = default)
    {
        entity.CreatedBy = entity.UpdatedBy = _context.GetUserName();
        entity.CreatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.IsDeleted = false;
        await _dbSet.AddAsync(entity, cancellation);
        return entity;
    }

    public async Task<IList<TEntity>> AddRangeAsync(IList<TEntity> entities, bool createNewId = true, CancellationToken cancellation = default)
    {
        var username = _context.GetUserName();
        var now = DateTimeOffset.UtcNow;
        foreach (var item in entities)
        {
            item.CreatedBy = item.UpdatedBy = username;
            item.CreatedAt = now;
            item.UpdatedAt = now;
            item.IsDeleted = false;
        }

        await _dbSet.AddRangeAsync(entities, cancellation);
        return entities;
    }

    public void Update(TEntity entity)
    {
        _dbSet.Update(entity);
    }

    public void Delete(TEntity entity)
    {
        entity.IsActive = false;
        entity.IsDeleted = true;
        _dbSet.Update(entity);
    }

    public async Task<List<TEntity>> DeleteAsync(List<TEntity> entities, bool isDetached = false, CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        foreach (var item in entities)
        {
            item.IsDeleted = true;
            item.IsActive = false;
            item.UpdatedBy = _context.GetUserName();
            item.UpdatedAt = DateTimeOffset.UtcNow;
            _context.Entry(item).Property("CreatedBy").IsModified = false;
            _context.Entry(item).Property("CreatedAt").IsModified = false;
            _context.Attach(item);
            _context.Entry(item).State = EntityState.Modified;
        }

        return entities;
    }

    public async Task<bool> DeleteWithIdsAsync(List<Guid> entityIds, CancellationToken cancellation = default)
    {
        if (entityIds == null || entityIds.Count == 0)
            throw new ArgumentNullException(nameof(entityIds));

        var entities = await _dbSet.Where(e => entityIds.Contains(e.Id)).ToListAsync(cancellation);
        if (entities.Count == 0)
            return false;

        var username = _context.GetUserName();
        var now = DateTimeOffset.UtcNow;
        foreach (var item in entities)
        {
            item.IsDeleted = true;
            item.IsActive = false;
            item.UpdatedBy = username;
            item.UpdatedAt = now;
            _context.Entry(item).Property("CreatedBy").IsModified = false;
            _context.Entry(item).Property("CreatedAt").IsModified = false;
            _context.Attach(item);
            _context.Entry(item).State = EntityState.Modified;
        }

        return true;
    }
}
