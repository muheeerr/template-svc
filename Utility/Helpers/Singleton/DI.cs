using Helpers.Singletons;
using Microsoft.Extensions.DependencyInjection;
using Utility.Helpers.Singleton.NotificationSender;

namespace Utility.Helpers.Singleton
{
    public static class DI
    {
        public static IServiceCollection AddAllSingletons(this IServiceCollection services)
        {
            services.AddSingleton(ReadGRPCEndpoints.Instance);
            var instance = NotificationSingleton.Instance;
            var grpcEndpoints = ReadGRPCEndpoints.Instance;
            instance.Initialize(grpcEndpoints);
            Console.WriteLine("notification is configured");
            services.AddSingleton<NotificationSingleton>(instance);
            
            return services;
        }

        

        
    }
}
