using GameHub.Infrastructure.Data.Seed.Development;
using GameHub.Infrastructure.Data.Seed.Production;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameHub.Infrastructure.Data.Seed;

public sealed class ApplicationSeeder
{
    private readonly IEnumerable<IProductionDataSeeder> _productionSeeders;
    private readonly IEnumerable<IDevelopmentDataSeeder> _developmentSeeders;
    private readonly ApplicationDbContext _context;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ApplicationSeeder> _logger;

    public ApplicationSeeder(
        IEnumerable<IProductionDataSeeder> productionSeeders,
        IEnumerable<IDevelopmentDataSeeder> developmentSeeders,
        ApplicationDbContext context,
        IHostEnvironment environment,
        ILogger<ApplicationSeeder> logger)
    {
        _productionSeeders = productionSeeders;
        _developmentSeeders = developmentSeeders;
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting database seeding for environment {EnvironmentName}.", _environment.EnvironmentName);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var seeder in _productionSeeders)
            {
                await seeder.SeedAsync(cancellationToken);
            }

            if (_environment.IsDevelopment())
            {
                foreach (var seeder in _developmentSeeders)
                {
                    await seeder.SeedAsync(cancellationToken);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Database seeding failed. Transaction rolled back.");
            throw;
        }
    }
}