using GameHub.Infrastructure.Data;
using GameHub.Infrastructure.Data.Seed;
using GameHub.WebAPI.Middleware;
using Microsoft.EntityFrameworkCore;

namespace GameHub.WebAPI.Extensions;

public static class ApplicationBuilderExtensions
{
    public static void UseCustomExceptionHandler(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
    }

    public static async Task RecreateDatabaseWithMigrationsAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;

        var logger = services.GetRequiredService<ILogger<Program>>();
        var context = services.GetRequiredService<ApplicationDbContext>();

        try
        {
            //logger.LogInformation("Starting database recreation process.");

            //logger.LogInformation("Deleting existing database...");
            //await context.Database.EnsureDeletedAsync();
            //logger.LogInformation("Database deleted successfully.");

            logger.LogInformation("Applying migrations...");
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully.");

            logger.LogInformation("Database recreation process completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "An error occurred while recreating the database with migrations."
            );

            throw;
        }
    }

    public static async Task SeedDataAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;
        var appSeeder = services.GetRequiredService<ApplicationSeeder>();
        await appSeeder.SeedAsync();
    }

}
