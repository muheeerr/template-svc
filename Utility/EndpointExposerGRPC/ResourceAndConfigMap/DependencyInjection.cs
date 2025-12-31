using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Utility.EndpointController;
using Utility.EndpointExposerGRPC.Server;

namespace Utility.EndpointExposerGRPC.ResourceAndConfigMap
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddEndpointExposerGrpc(this IServiceCollection services, string apiBaseName, Assembly assembly, Type iFeatureType)
        {
            services.AddSingleton(sp => new ActionExposedImpl(apiBaseName, assembly, iFeatureType));
            return services;
        }
    }
}
