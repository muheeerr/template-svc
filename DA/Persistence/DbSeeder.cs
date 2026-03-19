using DA.Seeding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DA.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
        if (!environment.IsDevelopment())
            return;

        var seedDatabase = bool.TryParse(
            Environment.GetEnvironmentVariable("SEED_DATABASE"), out var val) && val;

        if (!seedDatabase) return;

        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("DbSeeder");
        logger.LogInformation("Running database seeders...");

        using var scope = serviceProvider.CreateScope();
        var seeders = scope.ServiceProvider
            .GetServices<IDataSeeder>()
            .OrderBy(s => s.Order);

        foreach (var seeder in seeders)
            await seeder.SeedAsync();

        logger.LogInformation("Database seeding complete.");
    }
}
