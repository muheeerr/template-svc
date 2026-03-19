using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Utility.Logger
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddCustomLogger(this IServiceCollection services)
        {
            services.AddScoped<ICustomLogger>(options =>
                new LoggerImpl(
                    new LoggerConfiguration()
                        .WriteTo.File("logs/app-Logs.txt", rollingInterval: RollingInterval.Day)
                        .CreateLogger()
                ));

            services.AddHttpClient("slack");
            services.AddSingleton<SlackExceptionLogger>();

            Console.WriteLine($"[Info]----->{nameof(AddCustomLogger)} service added");
            return services;
        }
    }
}
