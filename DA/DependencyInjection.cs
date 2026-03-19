using DA.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DA;

public static class DependencyInjection
{
    public static IServiceCollection AddDALayer(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddDbContext(configuration)
            .AddUOW();
        Console.WriteLine($"[Info]----->{nameof(AddDALayer)} service added");
        return services;
    }
    public static IServiceCollection AddDbContext(this IServiceCollection services, IConfiguration configuration)
    {

        var DBhost = Environment.GetEnvironmentVariable("DBHost");
        ArgumentException.ThrowIfNullOrWhiteSpace(DBhost, "DBHost environment variable is required.");

        services.AddDbContext<AppDbContext>(
            options => options.UseNpgsql(DBhost)
            );
        return services;
    }
    public static IServiceCollection AddUOW(this IServiceCollection services)
    {
        services.TryAddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
