using DA;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddDALayer(configuration)
            .AddServices();

        // TODO: Register your services here
        // Example: services.AddTransient<YourService>();

        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Transient;
            options.Assemblies = [typeof(DependencyInjection)];
        });

        Console.WriteLine($"[Info]----->{nameof(AddBusinessLayer)} service added");
        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        // TODO: Register your scoped services here
        // Example: services.AddScoped<IYourService, YourService>();

        Console.WriteLine($"[Info]----->{nameof(AddServices)} service added");
        return services;
    }
}
