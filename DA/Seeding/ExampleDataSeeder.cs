using DA.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DA.Seeding;

public class ExampleDataSeeder(AppDbContext db, ILogger<ExampleDataSeeder> logger) : IDataSeeder
{
    public int Order => 1;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        // TODO: Add entity-specific seed logic here
        // Example: only seed if table is empty
        // if (await db.Set<YourEntity>().AnyAsync(ct))
        // {
        //     logger.LogInformation("YourEntity already has data — skipping seed.");
        //     return;
        // }
        //
        // db.Set<YourEntity>().Add(new YourEntity { ... });
        // await db.SaveChangesAsync(ct);

        logger.LogInformation("ExampleDataSeeder executed — add seed logic here.");
        await Task.CompletedTask;
    }
}
