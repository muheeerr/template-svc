using DA.Entities;
using DA.Persistence.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Collections.Concurrent;

namespace DA.Persistence
{
    /// <summary>
    /// Unit of work pattern wrapping AppDbContext with transaction support.
    /// <para>
    /// Add typed repository properties for each aggregate root:
    /// <code>
    /// // In IUnitOfWork:
    /// IRepository&lt;Order, AppDbContext&gt; Orders { get; }
    /// 
    /// // In UnitOfWork:
    /// public IRepository&lt;Order, AppDbContext&gt; Orders =&gt; GetRepository&lt;Order, AppDbContext&gt;();
    /// </code>
    /// </para>
    /// </summary>
    public interface IUnitOfWork : IDisposable, IAsyncDisposable
    {
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

        // Add typed repository properties here. Example:
        // public IRepository<Order, AppDbContext> Orders => GetRepository<Order, AppDbContext>();

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

        public virtual async ValueTask DisposeAsync()
        {
            await _db.DisposeAsync();
            GC.SuppressFinalize(this);
        }
        #endregion Commit and Dispose
    }
}
