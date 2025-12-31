using DA.Entities;
using DA.Persistence.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Collections.Concurrent;

namespace DA.Persistence
{
    public interface IUnitOfWork : IDisposable
    {
        // TODO: Add your repository properties here
        // Example: IRepository<YourEntity, AppDbContext> YourEntities { get; }

        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync();
        int Commit();
        Task<int> CommitAsync(CancellationToken cancellationToken = default);
    }

    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _db;
        private IDbContextTransaction? _transaction;
        private readonly ConcurrentDictionary<Type, object> _repositories = new();

        public UnitOfWork(AppDbContext db)
        {
            _db = db;
        }
        private IRepository<TEntity, TContext> GetRepository<TEntity, TContext>()
            where TEntity : Entity
            where TContext : BaseContext
        {
            return (IRepository<TEntity, TContext>)_repositories.GetOrAdd(
                typeof(TEntity),
                _ => new Repository<TEntity, TContext>((TContext)(object)_db)
            );
        }

        // TODO: Add your repository property implementations here
        // Example: public IRepository<YourEntity, AppDbContext> YourEntities => GetRepository<YourEntity, AppDbContext>();

        #region Commit and Dispose
        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null) throw new InvalidOperationException("Transaction already started");
            var strategy = _db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () => { _transaction = await _db.Database.BeginTransactionAsync(cancellationToken); });
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null) throw new InvalidOperationException("No transaction started");
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                await _transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                await DisposeTransactionAsync();
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction == null) return;
            try
            {
                await _transaction.RollbackAsync();
            }
            finally
            {
                await DisposeTransactionAsync();
            }
        }

        private async Task DisposeTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                await CommitTransactionAsync(cancellationToken);
                return 1;
            }
            return await _db.SaveChangesAsync(cancellationToken);
        }

        public int Commit()
        {
            if (_transaction != null) throw new NotSupportedException("Synchronous commit when transaction is active is not supported in this version");
            return _db.SaveChanges();
        }

        public virtual void Dispose()
        {
            _db.Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion Commit and Dispose
    }
}
