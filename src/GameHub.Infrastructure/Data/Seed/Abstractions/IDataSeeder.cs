namespace GameHub.Infrastructure.Data.Seed.Abstractions;

public interface IDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
