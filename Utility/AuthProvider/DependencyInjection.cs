using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;
using Utility.AuthProvider.AESEncryption;

namespace Utility.AuthProvider
{
    public static class DependencyInjection
    {
        
        public static IServiceCollection AddAuthProvider(this IServiceCollection services, IConfiguration configuration)
        {
            services.TryAddSingleton<ICustomAESEncryption, CustomAESEncryption>(); 
            Log.Information("[DI] {ServiceName} registered", nameof(AddAuthProvider));
            return services;
        }

        
    }
}
