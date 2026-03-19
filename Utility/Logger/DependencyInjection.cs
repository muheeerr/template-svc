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

            services.AddHttpClient("slack")
                .AddStandardResilienceHandler(options =>
                {
                    options.Retry.MaxRetryAttempts = 3;
                    options.Retry.Delay = TimeSpan.FromMilliseconds(500);
                    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
                    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
                });
            services.AddSingleton<SlackExceptionLogger>();

            Log.Information("[DI] {ServiceName} registered", nameof(AddCustomLogger));
            return services;
        }
    }
}
