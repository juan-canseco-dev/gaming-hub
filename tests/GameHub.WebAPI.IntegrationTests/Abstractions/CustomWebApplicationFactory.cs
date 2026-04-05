using GameHub.Application.Features.Chats.Consumers;
using GameHub.Infrastructure.Data;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Respawn;
using System.Data.Common;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace GameHub.WebAPI.IntegrationTests.Abstractions;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{

    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:latest")
        .WithPassword("Password01")
        .Build();

    // Added Redis test container
    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:latest")
        .Build();

    private DbConnection _dbConnection = null!;
    private Respawner _respawner = null!;

    public HttpClient HttpClient { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        await _redisContainer.StartAsync();

        HttpClient = CreateClient();

        using (var scope = Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.EnsureCreatedAsync();
        }

        _dbConnection = new SqlConnection(_dbContainer.GetConnectionString());
        await _dbConnection.OpenAsync();

        await InitializeRespawnerAsync();
    }

    public async new Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();

        if (_dbConnection != null)
        {
            await _dbConnection.DisposeAsync();
        }
    }

    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_dbConnection);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {

        builder.UseEnvironment("IntegrationTesting");

        Environment.SetEnvironmentVariable("ConnectionStrings:ConnectionString", _dbContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("ConnectionStrings:Redis", _redisContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("Jwt:SecretKey", "3bc3ecfd4d6d0855e7df9b43b452ebcfab80b1ae368873721e7b6e0fe70e1756");
        Environment.SetEnvironmentVariable("Jwt:Issuer", "https://test-issuer");
        Environment.SetEnvironmentVariable("Jwt:Audience", "https://test-audience");
        Environment.SetEnvironmentVariable("Cors:PolicyName", "TestCorsPolicy");
        Environment.SetEnvironmentVariable("Cors:AllowedOrigins", "https://localhost:5001");

        builder.ConfigureServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.ValidateIssuerSigningKey = false;
                options.TokenValidationParameters.ValidateIssuer = false;
                options.TokenValidationParameters.ValidateAudience = false;
                options.TokenValidationParameters.ValidateLifetime = false;
                options.TokenValidationParameters.RequireSignedTokens = false;
            });

            services.AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<ChatMessageSentConsumer>();
                cfg.AddConsumer<ChatMemberJoinedConsumer>();
            });
        });
    }

    private async Task InitializeRespawnerAsync()
    {
        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            TablesToIgnore = ["__EFMigrationsHistory"],
            DbAdapter = DbAdapter.SqlServer
        });
    }
}
