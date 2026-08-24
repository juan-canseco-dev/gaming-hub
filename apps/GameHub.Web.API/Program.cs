using Carter;
using GameHub.Application;
using GameHub.Infrastructure;
using GameHub.Infrastructure.Hubs;
using GameHub.Web.API.Configuration;
using GameHub.Web.API.Extensions;
using GameHub.Application.Abstractions.Observability;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace GameHub.Web.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            try
            {
                var builder = WebApplication.CreateBuilder(args);

                builder.Configuration
                    .AddJsonFile("appsettings.json", optional: false)
                    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);

                builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
                    .ReadFrom.Configuration(builder.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", "GameHub.Web.API")
                    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName));

                builder.Logging.Configure(options => options.ActivityTrackingOptions =
                    ActivityTrackingOptions.TraceId |
                    ActivityTrackingOptions.SpanId |
                    ActivityTrackingOptions.ParentId);

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

                var corsSection = builder.Configuration.GetSection(CorsOptions.SectionName);
                builder.Services.Configure<CorsOptions>(corsSection);

                var corsOptions = corsSection.Get<CorsOptions>();

                builder.Services.AddCors(options =>
                {
                    options.AddPolicy(corsOptions!.PolicyName, policy =>
                    {
                        policy.WithOrigins(corsOptions.AllowedOrigins)
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .WithExposedHeaders(CorrelationIdConstants.HeaderName);
                    });
                });

                var app = builder.Build();

                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
                {
                    app.MapOpenApi();
                    await app.ApplyMigrationsAsync();
                }
                if (!app.Environment.IsEnvironment("IntegrationTesting"))
                {
                    await app.SeedDataAsync();
                }

                app.UseCorrelationId();
                app.UseSerilogRequestLogging(options =>
                {
                    options.MessageTemplate =
                        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
                    options.GetLevel = (context, _, exception) =>
                        exception is not null || context.Response.StatusCode >= 500
                            ? LogEventLevel.Error
                            : context.Response.StatusCode >= 400
                                ? LogEventLevel.Warning
                                : LogEventLevel.Information;
                    options.EnrichDiagnosticContext = (diagnosticContext, context) =>
                    {
                        diagnosticContext.Set("CorrelationId", context.TraceIdentifier);
                        diagnosticContext.Set("RequestHost", context.Request.Host.Value ?? string.Empty);
                        diagnosticContext.Set("RequestScheme", context.Request.Scheme);
                    };
                });
                app.UseCustomExceptionHandler();

                app.UseCors(corsOptions!.PolicyName);
                app.UseAuthentication();
                app.UseAuthorization();

                app.MapGet("/", () => "Welcome to Game Hub");

                app.MapHub<ChatHub>("/hubs/chat");
                app.MapCarter();

                Log.Information("Starting GameHub API");
                await app.RunAsync();
            }
            catch (Exception exception)
            {
                Log.Fatal(exception, "GameHub API terminated unexpectedly");
                throw;
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }
    }
}
