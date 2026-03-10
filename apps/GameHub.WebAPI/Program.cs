
using Carter;
using GameHub.Application;
using GameHub.Infrastructure;
using GameHub.WebAPI.Extensions;

namespace GameHub.WebAPI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);


            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddApplication();
            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddAuthorization();
            builder.Services.AddCarter();
            builder.Services.AddJwtAuthentication(builder.Configuration);

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                await app.RecreateDatabaseWithMigrationsAsync();
            }

            await app.SeedDataAsync();

            app.UseHttpsRedirection();

            app.UseCustomExceptionHandler();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapCarter();
            app.Run();
        }
    }
}
