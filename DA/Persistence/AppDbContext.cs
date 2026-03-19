using DA.Auditing;
using DA.Entities;
using DA.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;
using System.Reflection;


namespace DA.Persistence
{
    public class AppDbContext(DbContextOptions<AppDbContext> dbContextOptions, IServiceProvider? serviceProvider = null) : BaseContext(dbContextOptions)
    {
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("uuid-ossp");
            modelBuilder.HasPostgresExtension("pgcrypto");

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var entity = modelBuilder.Entity(entityType.ClrType);

                // Configure Id property for UUID v7
                var idProperty = entityType.FindProperty(nameof(Entity.Id));
                if (idProperty != null)
                {
                    entity.Property(nameof(Entity.Id))
                        .HasColumnType("uuid")
                        .HasDefaultValueSql("uuid_generate_v7()");
                }

                // Set default values for specific properties
                var isDeletedProperty = entityType.FindProperty(nameof(Entity.IsDeleted));
                if (isDeletedProperty != null)
                {
                    isDeletedProperty.SetDefaultValue(false);
                    entity.HasQueryFilter(BuildIsDeletedFilter(entityType.ClrType));
                }

                var isActiveProperty = entityType.FindProperty(nameof(Entity.IsActive));
                isActiveProperty?.SetDefaultValue(true);
                if (isActiveProperty != null)
                {
                    entity.HasQueryFilter(BuildIsActiveFilter(entityType.ClrType));
                }

                var createdDateProperty = entityType.FindProperty(nameof(Entity.CreatedAt));
                createdDateProperty?.SetDefaultValueSql("CURRENT_TIMESTAMP(6)");
            }

            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }

        private static LambdaExpression BuildIsDeletedFilter(Type entityType)
        {
            var parameter = Expression.Parameter(entityType, "e");
            var propertyExpression = Expression.Property(parameter, nameof(Entity.IsDeleted));
            var body = Expression.Equal(propertyExpression, Expression.Constant(false));
            return Expression.Lambda(body, parameter);
        }

        private static LambdaExpression BuildIsActiveFilter(Type entityType)
        {
            var parameter = Expression.Parameter(entityType, "e");
            var propertyExpression = Expression.Property(parameter, nameof(Entity.IsActive));
            var body = Expression.Equal(propertyExpression, Expression.Constant(true));
            return Expression.Lambda(body, parameter);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
            }
        }

        public override int SaveChanges()
        {
            HandleAuditing();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            HandleAuditing();

            // Collect domain events before saving (entities may be detached after save)
            var entitiesWithEvents = ChangeTracker.Entries<Entity>()
                .Where(e => e.Entity.DomainEvents.Count > 0)
                .Select(e => e.Entity)
                .ToList();
            var domainEvents = entitiesWithEvents.SelectMany(e => e.DomainEvents).ToList();
            foreach (var entity in entitiesWithEvents)
                entity.ClearDomainEvents();

            var result = await base.SaveChangesAsync(cancellationToken);

            // Dispatch domain events after successful save
            if (domainEvents.Count > 0 && serviceProvider is not null)
            {
                using var scope = serviceProvider.CreateScope();
                var dispatchers = scope.ServiceProvider.GetServices<DA.Events.IDomainEventDispatcher>();
                foreach (var dispatcher in dispatchers)
                    await dispatcher.DispatchAsync(domainEvents, cancellationToken);
            }

            return result;
        }

        private void HandleAuditing()
        {
            var username = GetUserName();
            var now = DateTimeOffset.UtcNow;

            foreach (var entry in ChangeTracker.Entries<IAuditable>())
            {
                if (entry.State == EntityState.Added)
                {
                    if (string.IsNullOrWhiteSpace(entry.Entity.CreatedBy))
                        entry.Entity.CreatedBy = username;
                    if (entry.Entity.CreatedAt == default)
                        entry.Entity.CreatedAt = now;
                }

                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedBy = username;
                    entry.Entity.UpdatedAt = now;
                }
            }
        }
    }
}
