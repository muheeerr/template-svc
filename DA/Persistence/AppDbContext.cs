using DA.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;


namespace DA.Persistence
{
    public class AppDbContext(DbContextOptions<AppDbContext> dbContextOptions) : BaseContext(dbContextOptions)
    {
        // TODO: Add your DbSet properties here
        // Example: public DbSet<YourEntity> YourEntities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // modelBuilder.HasDefaultSchema("pharmacy");
            modelBuilder.HasPostgresExtension("uuid-ossp");
            modelBuilder.HasPostgresExtension("pgcrypto");

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var entity = modelBuilder.Entity(entityType.ClrType);

                //Configure Id property for UUID v7

                var idProperty = entityType.FindProperty(nameof(Entity.Id));
                if (idProperty != null)
                {
                    entity.Property(nameof(Entity.Id))
                        .HasColumnType("uuid")
                        .HasDefaultValueSql("uuid_generate_v7()")
                        .HasColumnName("id");
                }

                entityType.SetTableName(ToSnakeCase(entityType.GetTableName()!));
                foreach (var property in entityType.GetProperties())
                {
                    property.SetColumnName(ToSnakeCase(property.Name));
                }
                // Set default values for specific properties
                var isDeletedProperty = entityType.FindProperty(nameof(Entity.IsDeleted));
                if (isDeletedProperty != null)
                {
                    isDeletedProperty.SetDefaultValue(false);
                    // Implement query filter if necessary
                    entity.HasQueryFilter(BuildIsDeletedFilter(entityType.ClrType));
                }

                var isActiveProperty = entityType.FindProperty(nameof(Entity.IsActive));
                isActiveProperty?.SetDefaultValue(true);
                if (isActiveProperty != null)
                {
                    // Implement query filter if necessary
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
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void HandleAuditing()
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                var entity = entry.Entity;
                if (entity == null) continue;

                var type = entity.GetType();
                var username = GetUserName();
                var now = DateTimeOffset.UtcNow;
                if (entry.State == EntityState.Added)
                {
                    SetIfNullOrEmpty(type, entity, nameof(Entity.CreatedBy), username);
                    SetIfDefault(type, entity, nameof(Entity.CreatedAt), now);
                }

                if (entry.State == EntityState.Modified)
                {
                    SetIfNullOrEmpty(type, entity, nameof(Entity.UpdatedBy), username);
                    SetIfDefault(type, entity, nameof(Entity.UpdatedAt), now);
                }
                var properties = entity.GetType().GetProperties()
                    .Where(p => p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?) ||
                    p.PropertyType == typeof(DateTimeOffset?) || p.PropertyType == typeof(DateTimeOffset));

                foreach (var property in properties)
                {
                    if (property.GetValue(entity) is DateTimeOffset dateTime)
                    {
                        property.SetValue(entity, dateTime.ToUniversalTime());
                    }
                }
            }
        }
        private void SetIfNullOrEmpty(Type type, object entity, string propertyName, string value)
        {
            var prop = type.GetProperty(propertyName);
            if (prop != null && prop.CanWrite && prop.PropertyType == typeof(string))
            {
                var currentValue = prop.GetValue(entity) as string;
                //if (string.IsNullOrWhiteSpace(currentValue))
                {
                    prop.SetValue(entity, value);
                }
            }
        }

        private void SetIfDefault(Type type, object entity, string propertyName, DateTimeOffset value)
        {
            var prop = type.GetProperty(propertyName);
            if (prop != null && prop.CanWrite 
                && (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTimeOffset)))
            {
                var currentValue = prop.GetValue(entity)!;
                    prop.SetValue(entity, value);
            }
        }
        private static string ToSnakeCase(string input)
        {
            return string.Concat(
                input.Select((ch, i) =>
                    i > 0 && char.IsUpper(ch) ? "_" + ch : ch.ToString()
                )
            ).ToLower();
        }
    }
}
