using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Utility.NATSNotificationSystem
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddNatsService(this IServiceCollection services, IConfiguration configuration)
        { return services; }


        //public static IServiceCollection AddNatsService(this IServiceCollection services, IConfiguration configuration)
        //{
        //    services.AddSingleton<NatsConnectionManager>();

        //    services.AddSingleton<INatsService,NatsService>(provider =>
        //    {
        //        var connectionManager = provider.GetRequiredService<NatsConnectionManager>();
        //        return new NatsService(connectionManager);
        //    });

        //    //services.TryAddSingleton<INatsService, NatsService>();

        //    return services;
        //}
    }
}
