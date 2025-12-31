using Microsoft.AspNetCore.Builder;
using Utility.EndpointExposerGRPC.Server;

namespace Utility.EndpointExposerGRPC.ResourceAndConfigMap
{
    public static class ConfigureEndpointsExposed
    {
        public static void MapEndpointsExposed(this WebApplication app)
        {
            app.MapGrpcService<ActionExposedImpl>();
        }
    }
}