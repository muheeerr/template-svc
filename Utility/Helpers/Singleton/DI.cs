using Helpers.Singletons;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
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
            Log.Information("[DI] Notification singleton configured");
            services.AddSingleton<NotificationSingleton>(instance);
            
            return services;
        }

        

        
    }
}
