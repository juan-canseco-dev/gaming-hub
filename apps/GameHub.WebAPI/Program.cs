
using Carter;
using GameHub.Application;
using GameHub.Infrastructure;
using GameHub.WebAPI.Extensions;
using GameHub.Infrastructure.Hubs;

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
            var redisConnectionString = builder.Configuration.GetConnectionString("Redis") 
                ?? throw new InvalidOperationException("Redis connection string was not configured.");

            builder.Services
                .AddSignalR()
                .AddStackExchangeRedis(redisConnectionString);


            builder.Services.AddControllers();
            builder.Services.AddApplication();
            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddAuthorization();
            builder.Services.AddCarter();
            builder.Services.AddJwtAuthentication(builder.Configuration);

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();


            builder.Services.AddCors(options =>
            {
                options.AddPolicy("MyCors",
                    builder =>
                    {
                        builder.WithOrigins("https://localhost:7238")
                               .AllowAnyHeader()
                               .AllowAnyMethod();
                    });
            });



            var app = builder.Build();



            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                await app.RecreateDatabaseWithMigrationsAsync();
            }
            if (!app.Environment.IsEnvironment("IntegrationTesting"))
            {
                await app.SeedDataAsync();
            }


            app.UseHttpsRedirection();

            app.UseCustomExceptionHandler();

            app.UseCors("MyCors");
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapGet("/", () => "Welcome to Game Hub");
            
            app.MapHub<ChatHub>("/hubs/chat");
            app.MapCarter();
            app.Run();
        }
    }
}
