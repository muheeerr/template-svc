using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Utility.SessionManager
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddRedisSessionManagement(this IServiceCollection services, IConfiguration configuration)
        {

            var RedisHost = Environment.GetEnvironmentVariable("RedisHost");
            ArgumentNullException.ThrowIfNullOrEmpty(RedisHost, "please add env:RedisHost value");
            // Replace IConnectionMultiplexer in RedisSessionManager.cs with ConnectionMultiplexer
            services.AddSingleton<IConnectionMultiplexer>(x =>
            {
                var multiplexer = ConnectionMultiplexer.Connect(new ConfigurationOptions()
                {
                    EndPoints = { RedisHost }
                });

                return multiplexer;
            });

            services.AddScoped<RedisSessionManager>();

            return services;
        }
    }
}
