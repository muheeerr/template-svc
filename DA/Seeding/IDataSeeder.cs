namespace DA.Seeding;

public interface IDataSeeder
{
    int Order { get; }
    Task SeedAsync(CancellationToken ct = default);
}
