using GameHub.Infrastructure.Data;
using GameHub.Infrastructure.Data.Seed;
using GameHub.Web.API.Middleware;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Web.API.Extensions;

public static class ApplicationBuilderExtensions
{
    public static void UseCorrelationId(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
    }

    public static void UseCustomExceptionHandler(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
    }

    public static async Task ApplyMigrationsAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;

        var logger = services.GetRequiredService<ILogger<Program>>();
        var context = services.GetRequiredService<ApplicationDbContext>();

        try
        {
            logger.LogInformation("Applying database migrations");
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to apply database migrations"
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
