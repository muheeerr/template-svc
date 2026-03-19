using DA.Auditing;
using DA.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;

namespace DA;

public static class DependencyInjection
{
    public static IServiceCollection AddDALayer(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddDbContext(configuration)
            .AddUOW();

        // Audit logger
        services.AddScoped<IAuditLogger, DbAuditLogger>();

        // Auto-register all IDataSeeder implementations
        var seederTypes = System.Reflection.Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(DA.Seeding.IDataSeeder).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var seederType in seederTypes)
            services.AddTransient(typeof(DA.Seeding.IDataSeeder), seederType);

        Log.Information("[DI] {ServiceName} registered", nameof(AddDALayer));
        return services;
    }
    public static IServiceCollection AddDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var DBhost = Environment.GetEnvironmentVariable("DBHost");
        ArgumentException.ThrowIfNullOrWhiteSpace(DBhost, "DBHost environment variable is required.");

        services.AddDbContext<AppDbContext>(
            options => options.UseNpgsql(DBhost, npgsqlOpts =>
                npgsqlOpts.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null))
                .UseSnakeCaseNamingConvention());
        return services;
    }
    public static IServiceCollection AddUOW(this IServiceCollection services)
    {
        services.TryAddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
