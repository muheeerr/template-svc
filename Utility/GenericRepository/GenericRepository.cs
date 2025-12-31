using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;
using Utility.GenericRepository.CommonModels;



namespace Utility.GenericRepository
{
    public class GenericRepository<TEntity, PrimitiveType> : IGenericRepository<TEntity, PrimitiveType> where TEntity : Base<PrimitiveType>
    {
        private readonly DbContext _db;

        internal DbSet<TEntity> _dbSet;

        public GenericRepository(DbContext _context)
        {
            _db = _context;
            _dbSet = _db.Set<TEntity>();
        }

       

        public virtual SetterResult Add(TEntity entity, string createdBy)
        {
            try
            {
                
              /*  entity.IsActive = true;
                entity.IsArchived = false;*/

                _dbSet.Add(entity);
                return new SetterResult() { IsException = false, Result = true, Message = CommonMessages.Success };
            }
            catch (Exception e)
            {
                return new SetterResult() { IsException = true, Result = false, Message = e.ToString(), };

            }

        }

        public virtual async Task<SetterResult> AddAsync(TEntity entity, string createdBy,CancellationToken cancellationToken)
        {
            try
            {
               
                /*entity.IsActive = true;
                entity.IsArchived = false;*/
                await _dbSet.AddAsync(entity,cancellationToken);
                return new SetterResult() { IsException = false, Result = true, Message = CommonMessages.Success };
            }
            catch (Exception e)
            {
                return new SetterResult() { IsException = true, Result = false, Message = e.ToString() };

            }
        }

        public virtual SetterResult Delete(TEntity entity)
        {
            try
            {
                _dbSet.Remove(entity);
                return new SetterResult() { IsException = false, Result = true, Message = CommonMessages.Success };
            }
            catch (Exception e)
            {
                return new SetterResult() { IsException = true, Result = false, Message = e.ToString() };

            }
        }

        public virtual SetterResult Delete(PrimitiveType id)
        {
            try
            {
                var data = _dbSet.Find(id);
                if (data != null)
                {
                    _dbSet.Remove(data);
                    return new SetterResult() { IsException = false, Result = true, Message = CommonMessages.Success };
                }
                else
                {
                    return new SetterResult() { IsException = false, Result = false, Message = CommonMessages.NotFound };
                }


            }
            catch (Exception e)
            {
                return new SetterResult() { IsException = true, Result    = false, Message = e.ToString() };

            }
        }

        public virtual async Task<SetterResult> DeleteAsync(TEntity entity,CancellationToken cancellationToken)
        {
            try
            {

                _dbSet.Remove(entity);
                return new SetterResult() { IsException = false, Result = true, Message = CommonMessages.Success };


            }
            catch (Exception e)
            {
                return new SetterResult() { IsException = false, Result = false, Message = e.ToString() };

            }
        }

        public virtual async Task<SetterResult> DeleteAsync(PrimitiveType id, CancellationToken cancellationToken)
        {
            try
            {
                var data = await _dbSet.FindAsync(id,cancellationToken);
                if (data != null)
                {
                    _dbSet.Remove(data);
                    return new SetterResult() { Result = true, IsException = false, Message = CommonMessages.Success };
                }
                else
                {
                    return new SetterResult() { Result = false, IsException = false, Message = CommonMessages.NotFound };
                }


            }
            catch (Exception e)
            {
                return new SetterResult() { IsException = true, Result = false, Message = e.ToString() };

            }
        }
        public virtual async Task<SetterResult> DeleteRangeAsync(TEntity[] entities, CancellationToken cancellationToken)
        {
            try
            {

                _dbSet.RemoveRange(entities);
                return new SetterResult() { Result = true, IsException = false, Message = CommonMessages.Success };
            }
            catch (Exception e)
            {
                return new SetterResult() { IsException = true, Result = false, Message = e.ToString() };

            }
        }



        public virtual GetterResult<IEnumerable<TEntity>> Get(Expression<Func<TEntity, bool>> filter, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy, string includeProperties = "")
        {
            try
            {
                GetterResult<IEnumerable<TEntity>> getterResult = new GetterResult<IEnumerable<TEntity>>();
                getterResult.Message = CommonMessages.Success;
                getterResult.Status = true;
                IQueryable<TEntity> query = _dbSet;

                if (filter != null)
                {
                    query = query.Where(filter);
                }

                foreach (var includeProperty in includeProperties.Split
                    (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty);
                }

                if (orderBy != null)
                {
                    getterResult.Data = orderBy(query).ToList();
                }
                else
                {
                    getterResult.Data = query.ToList();
                }

                return getterResult;
            }
            catch (Exception e)
            {

                return new GetterResult<IEnumerable<TEntity>>() { Message = e.ToString(), Status = false };
            }
        }
        public virtual async Task<GetterResultPaginated<IEnumerable<TEntity>>> GetAsyncPaginated(CancellationToken cancellationToken,  string includeProperties = "", PaginationParameters? pagination = null)
        {
            pagination ??= new PaginationParameters();

            try
            {
                GetterResultPaginated<IEnumerable<TEntity>> getterResult = new GetterResultPaginated<IEnumerable<TEntity>>();
                getterResult.Message = CommonMessages.Success;
                getterResult.Status = true;
                IQueryable<TEntity> query = _dbSet;
                var totalRecord = 10;
                #region filter data
                query = query.Where(x => x.IsActive);
               /* if (pagination.FilterPropertyValue != "NA" && pagination.FilterPropertyName != "NA")
                {
                    query = query.Where(e => EF.Property<string>(e, pagination.FilterPropertyName) == pagination.FilterPropertyValue);
                }*/
                #endregion



                #region joining
                foreach (var includeProperty in includeProperties.Split
                    (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty);
                }
                #endregion

                totalRecord =await  _dbSet.CountAsync();


                if (pagination.Order == OrderRule.Asc)
                {
                    query = query.OrderBy(e => EF.Property<DateTime>(e, pagination.Property));
                }
                else
                {
                    query = query.OrderByDescending(e => EF.Property<DateTime>(e, pagination.Property));
                }

                #region cursoring

                if (pagination.Cursor != "NA")
                {
                    #region ordering

                   


                    #endregion

                    var propertyInfo = typeof(TEntity).GetProperty(pagination.Property);
                    if (propertyInfo != null)
                    {
                        DateTime time= DateTime.Parse(pagination.Cursor).ToUniversalTime();
                        
                        var cursorValue = Convert.ChangeType(time, propertyInfo.PropertyType);
                        var parameter = Expression.Parameter(typeof(TEntity), "e");
                        var propertyExpression = Expression.Property(parameter, propertyInfo);
                        var constantExpression = Expression.Constant(cursorValue);
                        Expression comparison = null;

                        if (pagination.Order == OrderRule.Asc)
                        {
                            comparison = Expression.GreaterThan(propertyExpression, constantExpression);
                        }
                        else
                        {
                            comparison = Expression.LessThan(propertyExpression, constantExpression);
                        }

                        var lambda = Expression.Lambda<Func<TEntity, bool>>(comparison, parameter);
                        query = query.Where(lambda);
                    
                    }

                }

                #endregion


                #region retrieving


                var pagedResults = await query.Take(pagination.PageSize).ToListAsync();

                
                #endregion

                #region make return 
                getterResult.Status = true;
                getterResult.Data = pagedResults;
                getterResult.Page = new PaginatedResponse()
                {
                    Cursor = pagedResults.Count > 0 ? pagedResults.Last().CreatedDate.ToString() : "NA",
                    PageSize = pagination.PageSize,
                    Order = pagination.Order,
                    Property = pagination.Property,
                    TotalCount = totalRecord,
                    FilterPropertyValue = pagination.FilterPropertyValue,
                    FilterPropertyName = pagination.FilterPropertyName
                };
                
                #endregion


                return getterResult;
            }
            catch (Exception e)
            {

                return new GetterResultPaginated<IEnumerable<TEntity>>() { Message = e.ToString(), Status = false };
            }
        }
        public virtual async Task<GetterResult<IEnumerable<TEntity>>> GetAsync(CancellationToken cancellationToken, Expression<Func<TEntity, bool>> filter , Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy, string includeProperties = "")
        {

            try
            {
                GetterResult<IEnumerable<TEntity>> getterResult = new GetterResult<IEnumerable<TEntity>>();
                getterResult.Message = CommonMessages.Success;
                getterResult.Status = true;
                IQueryable<TEntity> query = _dbSet;

                if (filter != null)
                {
                    query = query.Where(filter).Where(x=>x.IsActive);
                }

                foreach (var includeProperty in includeProperties.Split
                    (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty);
                }

                if (orderBy != null)
                {
                    getterResult.Data = await orderBy(query).ToListAsync(cancellationToken);
                }
                else
                {
                    getterResult.Data = await query.ToListAsync(cancellationToken);
                }

                return getterResult;
            }
            catch (Exception e)
            {

                return new GetterResult<IEnumerable<TEntity>>() { Message = e.ToString(), Status = false };
            }
        }
        public virtual  GetterResult<bool> Any( Expression<Func<TEntity, bool>> filter)
        {

            try
            {
                GetterResult<bool> getterResult = new GetterResult<bool>();
                getterResult.Message = CommonMessages.Success;
                getterResult.Status = true;
                IQueryable<TEntity> query = _dbSet;
                getterResult.Data =  query.Any(filter);
                return getterResult;
            }
            catch (Exception e)
            {

                return new GetterResult<bool>() { Message = e.ToString(), Status = false };
            }
        }
        public virtual async Task<GetterResult<bool>> AnyAsync(CancellationToken cancellationToken, Expression<Func<TEntity, bool>> filter )
        {
            try
            {
               GetterResult<bool > getterResult = new GetterResult<bool>();
                getterResult.Message = CommonMessages.Success;
                getterResult.Status = true;
                IQueryable<TEntity> query = _dbSet;
                if (filter != null)
                {
                    query = query.Where(filter).Where(x => x.IsActive);
                }
                else
                {
                    query = query.Where(x => x.IsActive);
                }
                getterResult.Data= await query.AnyAsync(filter,cancellationToken);
                return getterResult;
            }
            catch (Exception e)
            {

                return new GetterResult<bool>() { Message = e.ToString(), Status = false };
            }
        }

        public virtual GetterResult<IEnumerable<TEntity>> GetAll()
        {
            try
            {
                GetterResult<IEnumerable<TEntity>> getterResult = new GetterResult<IEnumerable<TEntity>>();
                getterResult.Message = CommonMessages.Success;
                getterResult.Status = true;
                getterResult.Data = _dbSet.AsEnumerable();
                return getterResult;
            }
            catch (Exception e)
            {

                return new GetterResult<IEnumerable<TEntity>>() { Message = e.Message, Status = false   };
            }

        }





        public virtual GetterResult<TEntity> GetById(PrimitiveType id)
        {
            try
            {
                var data = _dbSet.Find(id);
                GetterResult<TEntity> getterResult = new GetterResult<TEntity>();
                getterResult.Message = CommonMessages.Success;
                getterResult.Status = true;
                getterResult.Data = data;
                return getterResult;

            }
            catch (Exception e)
            {

                return new GetterResult<TEntity>() { Message = e.Message, Status = false };
            }
        }

        public virtual async Task<GetterResult<TEntity>> GetByIdAsync(PrimitiveType id,CancellationToken cancellationToken)
        {
            try
            {
                var data = await _dbSet.Where(entity => entity.Id.Equals(id) && entity.IsActive).FirstOrDefaultAsync(cancellationToken);
                GetterResult<TEntity> getterResult = new GetterResult<TEntity>();
                getterResult.Message = CommonMessages.Success;
                getterResult.Status = true;
                getterResult.Data = data;
                return getterResult;

            }
            catch (Exception e)
            {

                return new GetterResult<TEntity>() { Message = e.Message, Status = false, };
            }

        }

        public virtual GetterResult<IQueryable<TEntity>> GetQueryable()
        {
            try
            {

                GetterResult<IQueryable<TEntity>> getterResult = new GetterResult<IQueryable<TEntity>>();
                getterResult.Message = CommonMessages.Success;
                getterResult.Status = true;
                getterResult.Data = _dbSet.AsQueryable();
                return getterResult;

            }
            catch (Exception e)
            {

                return new GetterResult<IQueryable<TEntity>>() { Message = e.Message, Status = false };
            }
        }

        public virtual SetterResult Update(TEntity entity, string updatedBy)
        {
            try
            {
                entity.UpdateRecordStatus(DateTime.UtcNow, updatedBy);
                _dbSet.Update(entity);
                return new SetterResult() { Message = CommonMessages.Success, Result = true, IsException = false };
            }
            catch (Exception e)
            {
                return new SetterResult() { Message = e.ToString(), Result = false, IsException = true };
            }
        }
        //public virtual SetterResult UpdateOnCondition(Expression<Func<TEntity, bool>> filter, Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> setPropertyCalls)
        //{
        //    try
        //    {
        //        IQueryable<TEntity> query = _dbSet;
        //        int rowsEffected = query.Where(filter).ExecuteUpdate(setPropertyCalls);

        //        return new SetterResult() { Message = $"{CommonMessages.Success}.RowsEffected:{rowsEffected}", Result = true, IsException = false };
        //    }
        //    catch (Exception e)
        //    {
        //        return new SetterResult() { Message = e.ToString(), Result = false, IsException = true };
        //    }
        //}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="setPropertyCalls"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>int</returns>
        //public async virtual Task<SetterWithDataResult> UpdateOnConditionAsync(Expression<Func<TEntity, bool>> filter, Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> setPropertyCalls, CancellationToken cancellationToken)
        //{
        //    try
        //    {
        //        IQueryable<TEntity> query = _dbSet;
        //        int rowsEffected = await query.Where(filter).Where(x => x.IsActive).ExecuteUpdateAsync(setPropertyCalls, cancellationToken);
        //        return new SetterWithDataResult() { Message = $"{CommonMessages.Success}.RowsEffected:{rowsEffected}", Result = true, IsException = false,Data=rowsEffected };
        //    }
        //    catch (Exception e)
        //    {
        //        return new SetterWithDataResult() { Message = e.ToString(), Result = false, IsException = true };
        //    }
        //}
        public virtual SetterResult UpdateMany(TEntity[] entity)
        {
            try
            {

                _dbSet.UpdateRange(entity);
                return new SetterResult() { Message = CommonMessages.Success, Result = true, IsException = false };
            }
            catch (Exception e)
            {
                return new SetterResult() { Message = e.ToString(), Result = false, IsException = true };
            }
        }

        public virtual async Task<SetterResult> UpdateAsync(TEntity entity, string updatedBy, CancellationToken cancellationToken)
        {
            try
            {
                if (!entity.IsActive)
                {
                    return new SetterResult() { IsException = true, Result = false, Message = "Cannot update inactive entity." };
                }
                entity.UpdateRecordStatus(DateTime.UtcNow, updatedBy);
                _dbSet.Update(entity);

                return new SetterResult() { IsException = false, Result = true, Message = CommonMessages.Success, };
            }
            catch (Exception e)
            {
                return new SetterResult() { IsException = true, Result = false, Message = e.ToString(), };
            }
        }

        public virtual GetterResult<TEntity> GetSingle(Expression<Func<TEntity, bool>> filter, string includeProperties = "")
        {
            try
            {
                GetterResult<TEntity> getterResult = new GetterResult<TEntity>();
                getterResult.Message = CommonMessages.Success;
                getterResult.Status = true;
                IQueryable<TEntity> query = _dbSet;

                if (filter != null)
                {
                    query = query.Where(filter);
                }

                foreach (var includeProperty in includeProperties.Split
                    (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty);
                }

                getterResult.Data = query.FirstOrDefault();

                return getterResult;
            }
            catch (Exception e)
            {

                return new GetterResult<TEntity>() { Message = e.ToString(), Status = false };
            }
        }

        public virtual async Task<GetterResult<TEntity>> GetSingleAsync(CancellationToken cancellationToken,Expression<Func<TEntity, bool>> filter, string includeProperties = "")
        {
            try
            {
                GetterResult<TEntity> getterResult = new GetterResult<TEntity>();
                getterResult.Message = CommonMessages.Success;
                getterResult.Status = true;
                IQueryable<TEntity> query = _dbSet;
                if (filter != null)
                {
                    query = query.Where(filter).Where(x => x.IsActive);
                }
                else
                {
                    query = query.Where(x => x.IsActive); // Ensure only active records
                }
                foreach (var includeProperty in includeProperties.Split
                    (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty);
                }
                getterResult.Data = await query.FirstOrDefaultAsync(cancellationToken);
                return getterResult;
            }
            catch (Exception e)
            {

                return new GetterResult<TEntity>() { Message = e.ToString(), Status = false };
            }
        }

        public virtual async Task<GetterResult<IEnumerable<TEntity>>> GetAllAsync( CancellationToken cancellationToken)
        {
            try
            {
                GetterResult<IEnumerable<TEntity>> getterResult = new GetterResult<IEnumerable<TEntity>>();
                getterResult.Message = CommonMessages.Success;
                getterResult.Status = true;
                getterResult.Data = await _dbSet.Where(x => x.IsActive).ToListAsync(cancellationToken);
                return getterResult;
            }
            catch (Exception e)
            {

                return new GetterResult<IEnumerable<TEntity>>() { Message = e.Message, Status = false };
            }
        }

        public virtual GetterResult<bool> All(Expression<Func<TEntity, bool>> filter)
        {

            try
            {
                GetterResult<bool> getterResult = new GetterResult<bool>();
                getterResult.Message = CommonMessages.Success;
                getterResult.Status = true;
                IQueryable<TEntity> query = _dbSet;
                getterResult.Data = query.All(filter);
                return getterResult;
            }
            catch (Exception e)
            {

                return new GetterResult<bool>() { Message = e.ToString(), Status = false };
            }
        }
    }
}