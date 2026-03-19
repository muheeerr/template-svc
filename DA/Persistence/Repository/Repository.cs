using DA.Entities;
using DA.Specifications;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Utility.Helpers.Common;

namespace DA.Persistence.Repository;

public class Repository<TEntity, TContext> : IRepository<TEntity, TContext>
    where TEntity : Entity
    where TContext : BaseContext
{
    private readonly TContext _context;
    private readonly DbSet<TEntity> _dbSet;

    public Repository(TContext context)
    {
        _context = context;
        _dbSet = _context.Set<TEntity>();
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _context.GetUserName();
        entity.CreatedBy = _context.GetUserName();
        await _dbSet.AddAsync(entity);
    }
    public async Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(predicate);
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

    public async Task SaveChangesAsync()
    {
        //await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellation = default)
    {
        return await _dbSet.AsNoTracking().OrderByDescending(d => d.CreatedAt).ToListAsync(cancellation);
    }

    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellation = default)
    {
        return await _dbSet.FindAsync(id, cancellation);
    }

    public async Task<TEntity> AddAsync(TEntity entity, bool createNewId = true, CancellationToken cancellation = default)
    {
        entity.CreatedBy = entity.UpdatedBy = _context.GetUserName();
        entity.CreatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.IsDeleted = false;
        await _dbSet.AddAsync(entity, cancellation);
        //await _context.SaveChangesAsync();

        return entity;
    }

    public async Task<TEntity> AddDetachedAsync(TEntity entity, bool createNewId = true, CancellationToken cancellation = default)
    {
        entity.CreatedBy = entity.UpdatedBy = _context.GetUserName();
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.IsDeleted = false;
        await _dbSet.AddAsync(entity, cancellation);
        //await _context.SaveChangesAsync();
        _context.Entry(entity).State = EntityState.Detached;
        return entity;
    }

    public async Task<IList<TEntity>> AddAsync(IList<TEntity> entities, bool createNewId = true, CancellationToken cancellation = default)
    {
        int sec = 1;
        foreach (var item in entities)
        {
            item.CreatedBy = item.UpdatedBy = _context.GetUserName();
            item.CreatedAt = DateTime.UtcNow.AddSeconds(sec);
            item.UpdatedAt = DateTime.UtcNow.AddSeconds(sec);
            item.IsDeleted = false;
            await _dbSet.AddAsync(item, cancellation);
            sec++;
        }

        //await _context.SaveChangesAsync();
        return entities;
    }

    public TEntity AddSync(TEntity entity, CancellationToken cancellation = default)
    {
        entity.CreatedBy = entity.UpdatedBy = _context.GetUserName();
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.IsDeleted = false;
        _dbSet.AddAsync(entity, cancellation);
        //_context.SaveChangesAsync(cancellation);
        return entity;
    }

    public async Task<TEntity> UpdateAsync(TEntity entity, bool isDetached = false, CancellationToken cancellation = default)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _context.GetUserName();
        _dbSet.Attach(entity);
        _context.Entry(entity).State = EntityState.Modified;
        _context.Entry(entity).Property("CreatedBy").IsModified = false;
        _context.Entry(entity).Property("CreatedAt").IsModified = false;
        //await _context.SaveChangesAsync();
        if (isDetached)
        {
            _context.Entry(entity).State = EntityState.Detached;
        }

        return entity;
    }

    public async Task<List<TEntity>> UpdateAsync(List<TEntity> entities, bool isDetached = false, CancellationToken cancellation = default)
    {
        if (entities == null)
        {
            throw new ArgumentNullException(nameof(entities));
        }

        foreach (var item in entities)
        {
            item.UpdatedBy = _context.GetUserName();
            item.UpdatedAt = DateTime.UtcNow;
            _context.Entry(item).Property("CreatedBy").IsModified = false;
            _context.Entry(item).Property("CreatedAt").IsModified = false;
            _context.Attach(item);
            _context.Entry(item).State = EntityState.Modified;
        }

        //await _context.SaveChangesAsync();
        return entities;
    }
    public async Task<List<TEntity>> DeleteAsync(List<TEntity> entities, bool isDetached = false, CancellationToken cancellation = default)
    {
        if (entities == null)
        {
            throw new ArgumentNullException(nameof(entities));
        }

        foreach (var item in entities)
        {
            item.IsDeleted = true;
            item.IsActive = false;
            item.UpdatedBy = _context.GetUserName();
            item.UpdatedAt = DateTime.UtcNow;
            _context.Entry(item).Property("CreatedBy").IsModified = false;
            _context.Entry(item).Property("CreatedAt").IsModified = false;
            _context.Attach(item);
            _context.Entry(item).State = EntityState.Modified;
        }

        //await _context.SaveChangesAsync();
        return entities;
    }
    
    public async Task<bool> DeleteWithIdsAsync(List<Guid> entityIds, CancellationToken cancellation = default)
    {
        if (entityIds == null || !entityIds.Any())
        {
            throw new ArgumentNullException(nameof(entityIds));
        }

        var entities = await _dbSet.Where(e => entityIds.Contains(e.Id)).ToListAsync(cancellation);
        if (!entities.Any())
        {
            return false;
        }
        foreach (var item in entities)
        {
            item.IsDeleted = true;
            item.IsActive = false;
            item.UpdatedBy = _context.GetUserName();
            item.UpdatedAt = DateTime.UtcNow;
            _context.Entry(item).Property("CreatedBy").IsModified = false;
            _context.Entry(item).Property("CreatedAt").IsModified = false;
            _context.Attach(item);
            _context.Entry(item).State = EntityState.Modified;
        }

        //await _context.SaveChangesAsync();
        return true;
    }


    public async Task<TEntity> DeleteAsync(Guid Id, string UpdatedBy = "", CancellationToken cancellation = default)
    {
        var data = await _dbSet.FindAsync(Id, cancellation);
        if (data == null)
        {
            return null!;
        }

        data.UpdatedBy = _context.GetUserName();
        if (UpdatedBy != "")
        {
            data.UpdatedBy = UpdatedBy;
        }

        data.UpdatedAt = DateTime.UtcNow;
        data.IsDeleted = true;
        //await _context.SaveChangesAsync();
        return data;
    }

    public async Task<TEntity> HardDeleteAsync(Guid Id, CancellationToken cancellation = default)
    {
        var data = await _dbSet.FindAsync(Id, cancellation);
        if (data == null)
        {
            return null!;
        }

        _dbSet.Remove(data);
        //await _context.SaveChangesAsync();
        return data;
    }

    public async Task<bool> DeleteMultipleAsync(List<Guid> Ids, string UpdatedBy = "", CancellationToken cancellation = default)
    {
        foreach (var id in Ids)
        {
            var data = await _dbSet.FindAsync(id, cancellation);
            if (data == null)
            {
                continue;
            }

            data.UpdatedBy = _context.GetUserName();
            if (UpdatedBy != "")
            {
                data.UpdatedBy = UpdatedBy;
            }

            data.UpdatedAt = DateTime.UtcNow;
            data.IsDeleted = true;
        }

        //await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteMultipleAsync(Expression<Func<TEntity, bool>> where, string UpdatedBy = "", CancellationToken cancellation = default)
    {
        var data = await _dbSet.Where(where).ToListAsync(cancellation);
        foreach (var item in data)
        {
            item.UpdatedBy = _context.GetUserName();
            if (UpdatedBy != "")
            {
                item.UpdatedBy = UpdatedBy;
            }

            item.UpdatedAt = DateTime.UtcNow;
            item.IsDeleted = true;
        }

        //await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Get list of entities with pagination
    /// </summary>
    /// <param name="where">Predicate</param>
    /// <param name="PageNumber">Page Number (should be greater than 0)</param>
    /// <param name="PageSize">Page Size (should be greate than 0)</param>
    /// <param name="OrderBy">0=>ascending 1=>descending</param>
    /// <returns></returns>
    public async Task<(int, List<TEntity>)> GetListWithPagination(Expression<Func<TEntity, bool>> where, int PageNumber, int PageSize, int? OrderBy = 0, CancellationToken cancellation = default)
    {
        int skip = (PageNumber - 1) * PageSize;
        //int take = PageSize * PageNumber;
        int take = PageSize;
        var initial = _dbSet.AsNoTracking().Where(where).OrderByDescending(x => x.CreatedAt);
        var total = initial.Count();
        if (OrderBy == 1)
        {
            return (total, await initial.Skip(skip).Take(take).ToListAsync(cancellation));
        }
        else
        {
            return (total, await initial.Skip(skip).Take(take).ToListAsync(cancellation));
        }
    }

    /// <summary>
    /// Get list of entities with pagination
    /// </summary>
    /// <param name="PageNumber">Page Number (should be greater than 0)</param>
    /// <param name="PageSize">Page Size (should be greate than 0)</param>
    /// <param name="OrderBy">0=>ascending 1=>descending</param>
    /// <returns></returns>
    public async Task<(int, List<TEntity>)> GetAllWithPagination(int PageNumber, int PageSize, int? OrderBy = 0, CancellationToken cancellation = default)
    {
        int skip = (PageNumber - 1) * PageSize;
        int take = PageSize;
        var initial = _dbSet.AsNoTracking().OrderByDescending(x => x.CreatedAt);
        var total = initial.Count();
        if (OrderBy == 1)
        {
            return (total, await initial.Skip(skip).Take(take).ToListAsync(cancellation));
        }
        else
        {
            return (total, await initial.Skip(skip).Take(take).ToListAsync(cancellation));
        }
    }

    public IQueryable<TEntity> GetWithInclude(Expression<Func<TEntity, bool>> predicate, params string[] include)
    {
        bool includeIsDeleted = include.Contains("IsDeleted");
        include = include.Where(x => x != "IsDeleted").ToArray();
        IQueryable<TEntity> query = _dbSet;
        query = include.Aggregate(query, (current, inc) => current.Include(inc));
        if (includeIsDeleted)
        {
            return query.AsNoTracking().IgnoreQueryFilters().Where(predicate).OrderByDescending(d => d.CreatedAt);
        }
        else
        {
            return query.AsNoTracking().Where(predicate).Where(x => x.IsDeleted == false).OrderByDescending(d => d.CreatedAt);
        }
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
        include ??= Array.Empty<string>();
        IQueryable<TEntity> query = _dbSet.AsNoTracking();
        query = include.Aggregate(query, (current, inc) => current.Include(inc));

        // Apply soft delete logic
        if (includeDeleted)
            query = query.IgnoreQueryFilters().Where(predicate);
        else
            query = query.Where(predicate).Where(x => !x.IsDeleted);

        int totalCount = await query.CountAsync(ct);

        query = query.OrderByDescending(x => x.CreatedAt);

        IQueryable<TResult> projectedQuery;
        if (selector is null)
        {
            // Return entity itself if TResult == TEntity
            if (typeof(TResult) == typeof(TEntity))
            {
                projectedQuery = (IQueryable<TResult>)query;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Selector cannot be null when TResult ({typeof(TResult).Name}) differs from TEntity ({typeof(TEntity).Name})."
                );
            }
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
        include ??= Array.Empty<string>();
        IQueryable<TEntity> query = _dbSet.AsNoTracking();
        query = include.Aggregate(query, (current, inc) => current.Include(inc));

        // Apply soft delete logic
        if (includeDeleted)
            query = query.IgnoreQueryFilters().Where(predicate);
        else
            query = query.Where(predicate).Where(x => !x.IsDeleted);

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

    public (IQueryable<TEntity>, int) GetWithIncludePaginatedQueryAble(Expression<Func<TEntity, bool>> predicate, int PageNumber, int PageSize, params string[] include)
    {
        int skip = (PageNumber - 1) * PageSize;
        int take = PageSize;
        IQueryable<TEntity> query = _dbSet;
        query = include.Aggregate(query, (current, inc) => current.Include(inc));
        var total = query.AsNoTracking().Where(predicate).Count();
        return (query.AsNoTracking().Where(predicate).OrderByDescending(d => d.CreatedAt).Skip(skip).Take(take), total);
    }

    public (IQueryable<TEntity>, int) GetPaginatedQueryAble(Expression<Func<TEntity, bool>> predicate, int PageNumber, int PageSize)
    {
        int skip = (PageNumber - 1) * PageSize;
        int take = PageSize;
        IQueryable<TEntity> query = _dbSet;
        var total = query.AsNoTracking().Where(predicate).Count();
        return (query.AsNoTracking().Where(predicate).OrderByDescending(d => d.CreatedAt).Skip(skip).Take(take), total);
    }

    public (IQueryable<TEntity>, int) GetWithIncludeQueryAbleWithoutOrder(Expression<Func<TEntity, bool>> predicate, params string[] include)
    {
        IQueryable<TEntity> query = _dbSet;
        query = include.Aggregate(query, (current, inc) => current.Include(inc));
        var total = query.AsNoTracking().Where(predicate).Count();
        return (query.AsNoTracking().Where(predicate), total);
    }

    public async Task<(int, IList<TEntity>)> GetPaginationWithIncludeAsync(Expression<Func<TEntity, bool>> predicate, int PageNumber, int PageSize, int? OrderBy = 0, CancellationToken cancellation = default, params string[] include)
    {
        int skip = (PageNumber - 1) * PageSize;
        IQueryable<TEntity> query = _dbSet;
        query = include.Aggregate(query, (current, inc) => current.Include(inc));
        var initial = query.AsNoTracking().Where(predicate).OrderByDescending(d => d.CreatedAt);

        var total = initial.Count();
        if (OrderBy == 1)
        {
            return (total, await initial.Skip(skip).Take(PageSize).ToListAsync(cancellation));
        }
        else
        {
            return (total, await initial.Skip(skip).Take(PageSize).ToListAsync(cancellation));
        }
    }


    public async Task<TEntity?> GetOneDefaultWithInclude(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellation = default, params string[] include)
    {
        IQueryable<TEntity> query = _dbSet;
        query = include.Aggregate(query, (current, inc) => current.Include(inc));
        return await query.Where(predicate).FirstOrDefaultAsync(cancellation);
    }


    public async Task<bool> Exists(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellation = default)
    {
        return await _dbSet.Where(x => x.IsDeleted == false).AnyAsync(predicate, cancellation);
    }

    public async Task<bool> Exists(object primaryKey, CancellationToken cancellation = default)
    {
        return await _dbSet.FindAsync(primaryKey, cancellation) != null;
    }

    public async Task<List<TEntity>> GetMany(Expression<Func<TEntity, bool>> where, CancellationToken cancellation = default)
    {
        return await _dbSet.AsNoTracking().Where(where).Where(x => x.IsDeleted == false).OrderByDescending(d => d.CreatedAt).ToListAsync();
    }


    public IQueryable<TEntity> GetManyIQueryable(Expression<Func<TEntity, bool>> where)
    {
        return _dbSet.AsNoTracking().Where(where).Where(x => x.IsDeleted == false).AsQueryable().OrderByDescending(d => d.CreatedAt);
    }

    public IQueryable<TEntity> GetManyIQueryableWithDeleted(Expression<Func<TEntity, bool>> where)
    {
        return _dbSet.AsNoTracking().IgnoreQueryFilters().Where(where).AsQueryable().OrderByDescending(d => d.CreatedAt);
    }


    public async Task<TEntity> GetFirst(Expression<Func<TEntity, bool>> predicate, int? OrderBy = 1, CancellationToken cancellation = default)
    {
        if (OrderBy == 0)
        {
            return (await _dbSet.Where(x => x.IsDeleted == false).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(predicate, cancellation))!;
        }
        else
        {
            return (await _dbSet.Where(x => x.IsDeleted == false).FirstOrDefaultAsync(predicate))!;
        }
    }

    public async Task<TEntity?> GetByIdAsync(object shiftId, CancellationToken cancellation = default)
    {
        return await _dbSet.FindAsync(shiftId, cancellation);
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

    public async Task<TEntity?> GetLastOrDefaultWithInclude(
        Expression<Func<TEntity, bool>> predicate,
        bool includeDeleted = false,
        CancellationToken ct = default,
        params string[] include)
    {
        IQueryable<TEntity> query = _dbSet.AsNoTracking();
        query = include.Aggregate(query, (current, inc) => current.Include(inc));
        if (!includeDeleted)
            query = query.Where(x => !x.IsDeleted);
        return await query.Where(predicate).OrderBy(x => x.CreatedAt).LastOrDefaultAsync(ct);
    }

    public async Task<TResult?> GetLastOrDefaultWithInclude<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>> selector,
        bool includeDeleted = false,
        CancellationToken ct = default,
        params string[] include)
    {
        IQueryable<TEntity> query = _dbSet.AsNoTracking();
        query = include.Aggregate(query, (current, inc) => current.Include(inc));
        if (!includeDeleted)
            query = query.Where(x => !x.IsDeleted);
        return await query.Where(predicate).OrderBy(x => x.CreatedAt).Select(selector).LastOrDefaultAsync(ct);
    }


    //public async Task<int> ExecuteUpdateAsync(
    //Expression<Func<TEntity, bool>> filter,
    //Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>> setPropertyCalls)
    //{
    //    var now = DateTime.UtcNow;
    //    var user = _context.GetUserName() ?? "system";

    //    Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>> composed = calls =>
    //    {
    //        var result = setPropertyCalls(calls);

    //        result = result.SetProperty(e => e.UpdatedAt, _ => now);
    //        result = result.SetProperty(e => e.UpdatedBy, _ => user);

    //        return result;
    //    };

    //    return await _dbSet
    //        .Where(filter)
    //        .ExecuteUpdateAsync(composed);
    //}

    //public async Task<int> ExecuteUpdateAsync(
    //    Expression<Func<TEntity, bool>> filter,
    //    Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>> setPropertyCalls)
    //{
    //    IQueryable<TEntity> query = _dbSet;
    //    var now = DateTimeOffset.UtcNow;
    //    var user = _context.GetUserName();

    //    // In EF Core 10, ExecuteUpdateAsync uses Func instead of Expression, so we compose the Func directly
    //    Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>> composed = calls =>
    //    {
    //        var result = setPropertyCalls(calls);
    //        result = result.SetProperty(e => e.UpdatedAt, e => now);
    //        result = result.SetProperty(e => e.UpdatedBy, e => user);
    //        return result;
    //    };

    //    return await query.Where(filter).ExecuteUpdateAsync(composed);
    //}

    public async Task<List<TResult>> GetMany<TResult>(Expression<Func<TEntity, bool>> where, Expression<Func<TEntity, TResult>> selector, CancellationToken cancellation = default)
    {
        return await _dbSet.AsNoTracking().Where(where).Select(selector).ToListAsync(cancellation);
    }

    public async Task<PagedResult<TResult>> GetManyPaginated<TResult>(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellation = default)
    {
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

    public async Task<TEntity?> GetBySpecAsync(BaseSpecification<TEntity> spec, CancellationToken ct = default)
        => await SpecificationEvaluator<TEntity>.GetQuery(_context.Set<TEntity>().AsQueryable(), spec).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<TEntity>> ListBySpecAsync(BaseSpecification<TEntity> spec, CancellationToken ct = default)
        => await SpecificationEvaluator<TEntity>.GetQuery(_context.Set<TEntity>().AsQueryable(), spec).ToListAsync(ct);

    public async Task<int> CountBySpecAsync(BaseSpecification<TEntity> spec, CancellationToken ct = default)
        => await SpecificationEvaluator<TEntity>.GetQuery(_context.Set<TEntity>().AsQueryable(), spec).CountAsync(ct);
 }
