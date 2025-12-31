using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;
using Utility.GenericRepository.CommonModels;


namespace Utility.GenericRepository

{

    public interface IGenericRepository<TEntity, PrimitiveType> where TEntity : Base<PrimitiveType>
    {
        SetterResult Add(TEntity entity, string createdBy);

        SetterResult Update(TEntity entity, string updatedBy);
        SetterResult UpdateMany(TEntity[] entity);

        SetterResult Delete(TEntity entity);
        SetterResult Delete(PrimitiveType id);
        GetterResult<TEntity> GetById(PrimitiveType id);
        Task<GetterResult<IEnumerable<TEntity>>> GetAllAsync(CancellationToken cancellationToken);
        GetterResult<IEnumerable<TEntity>> GetAll();
        GetterResult<IEnumerable<TEntity>> Get(
             Expression<Func<TEntity, bool>> filter ,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy ,
            string includeProperties = ""
            );
        GetterResult<TEntity> GetSingle(
             Expression<Func<TEntity, bool>> filter ,
            string includeProperties = ""
            );
        GetterResult<IQueryable<TEntity>> GetQueryable();

        //asyncMethod
        Task<SetterResult> AddAsync(TEntity entity, string createdBy, CancellationToken cancellationToken);
        Task<SetterResult> UpdateAsync(TEntity entity, string updatedBy, CancellationToken cancellationToken);
        Task<SetterResult> DeleteAsync(TEntity entity, CancellationToken cancellationToken);
        Task<SetterResult> DeleteAsync(PrimitiveType id, CancellationToken cancellationToken);
        Task<GetterResult<TEntity>> GetByIdAsync(PrimitiveType id, CancellationToken cancellationToken);


        Task<GetterResult<IEnumerable<TEntity>>> GetAsync(
              CancellationToken cancellationToken,
             Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
              
            string includeProperties = ""
          
            );

        Task<SetterResult> DeleteRangeAsync(TEntity[] entities, CancellationToken cancellationToken);


        Task<GetterResultPaginated<IEnumerable<TEntity>>> GetAsyncPaginated(CancellationToken cancellationToken, string includeProperties = "", PaginationParameters? pagination = null);

        Task<GetterResult<TEntity>> GetSingleAsync(
              CancellationToken cancellationToken,
            Expression<Func<TEntity, bool>> filter ,
              
           string includeProperties = ""
           );

       Task<GetterResult<bool>> AnyAsync(CancellationToken cancellationToken, Expression<Func<TEntity, bool>> filter);

        GetterResult<bool> Any( Expression<Func<TEntity, bool>> filter);
        GetterResult<bool> All(Expression<Func<TEntity, bool>> filter);

    }
}