using Core.Events;
using DA;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Core;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddDALayer(configuration)
            .AddServices();

        // Domain event dispatcher (registered against DA interface so AppDbContext can resolve it)
        services.AddScoped<DA.Events.IDomainEventDispatcher, InProcessDomainEventDispatcher>();

        // TODO: Register your services here
        // Example: services.AddTransient<YourService>();

        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Transient;
            options.Assemblies = [typeof(DependencyInjection)];
        });

        Log.Information("[DI] {ServiceName} registered", nameof(AddBusinessLayer));
        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        // TODO: Register your scoped services here
        // Example: services.AddScoped<IYourService, YourService>();

        Log.Information("[DI] {ServiceName} registered", nameof(AddServices));
        return services;
    }
}
