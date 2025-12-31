using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Utility.Logger
{
    public static class DependencyInjection
    {
        private static IConfiguration? Configuration;
        public static IServiceCollection AddCustomLogger(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ICustomLogger>((options =>
            new LoggerImpl(
              new LoggerConfiguration()
                   .WriteTo.File("logs/app-Logs.txt", rollingInterval: RollingInterval.Day)
                    .CreateLogger()
            )));

            Configuration = configuration;

            Console.WriteLine($"[Info]----->{nameof(AddCustomLogger)} service added");


            return services;
        }

        public static string SlackWebhook
        {
            get
            {
                var slack=Environment.GetEnvironmentVariable("SlackWebHook");
                ArgumentNullException.ThrowIfNullOrEmpty(slack, "please add env:SlackWebHook value");
                return slack;
            }
        }
    }
}
