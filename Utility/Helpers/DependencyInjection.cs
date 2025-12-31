using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Utility.Helpers
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddEndpointGRPC(this IServiceCollection services, IConfiguration configuration, string ApiBaseName, Assembly assembly, Type iFeature)
        {
            services.AddGrpc();
            //services.AddTransient<ActionExposedImpl>(serviceProvider =>
            //{
            //    return new ActionExposedImpl(ApiBaseName, assembly, iFeature);
            //});

            return services;
        }
        public static IServiceCollection AddHelpers(this IServiceCollection services, IConfiguration configuration)
        {
            services.TryAddSingleton<IRead, Config>();
            Console.WriteLine($"[Info]----->{nameof(AddHelpers)} service added");
            return services;
        }
    }
}
