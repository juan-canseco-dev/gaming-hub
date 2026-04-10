using GameHub.Application.Abstractions.Authentication;
using GameHub.Application.Abstractions.Clock;
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Identity;
using GameHub.Application.Abstractions.Realtime.Chats;
using GameHub.Domain.Chats;
using GameHub.Domain.Channels;
using GameHub.Infrastructure.Authentication;
using GameHub.Infrastructure.Clock;
using GameHub.Infrastructure.Data;
using GameHub.Infrastructure.Data.Seed;
using GameHub.Infrastructure.Data.Seed.Development;
using GameHub.Infrastructure.Data.Seed.Production;
using GameHub.Infrastructure.Identity;
using GameHub.Infrastructure.Identity.Models;
using GameHub.Infrastructure.Realtime;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ApplicationDI = GameHub.Application.DependencyInjection;

namespace GameHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IDateTimeProvider, DateTimeProvider>();
        services.AddTransient<MessagePreviewService>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton<IAuthenticatedUserService, AuthenticatedUserService>();

        // Register Ef core dependencies 
        var connectionString = configuration.GetConnectionString("ConnectionString")
                          ?? throw new ArgumentNullException(nameof(configuration));
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString);
            options.EnableSensitiveDataLogging();
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+/ ";
            options.User.RequireUniqueEmail = true;
            options.Password.RequireDigit = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 6;
        })
       .AddRoles<ApplicationRole>()
       .AddEntityFrameworkStores<ApplicationDbContext>()
       .AddDefaultTokenProviders();

        services.AddScoped<IIdentityService, IdentityService>();


        // Register message broker 
        services.AddMassTransit(x =>
        {
            x.AddConsumers(typeof(ApplicationDI).Assembly);
            x.SetKebabCaseEndpointNameFormatter();

            x.AddEntityFrameworkOutbox<ApplicationDbContext>(o =>
            {
                o.UseSqlServer();
                o.UseBusOutbox();
            });

            x.AddConfigureEndpointsCallback((context, name, cfg) => {
                cfg.UseEntityFrameworkOutbox<ApplicationDbContext>(context);
            });


            x.UsingRabbitMq((context, cfg) =>
            {
                var hostUri = new Uri(configuration["EventBusSettings:Host"]!);
                cfg.Host(hostUri, h =>
                {
                    h.Username(configuration["EventBusSettings:Username"]!);
                    h.Password(configuration["EventBusSettings:Password"]!);
                });

                cfg.ConfigureEndpoints(context);
            });
        });


        // Add Application seeders
        services.AddScoped<IProductionDataSeeder, AdminUserSeeder>();
        services.AddScoped<IProductionDataSeeder, ChannelChatsSeeder>();
        services.AddScoped<IDevelopmentDataSeeder, DemoUsersSeeder>();
        services.AddScoped<IDevelopmentDataSeeder, DemoChatSeeder>();
        services.AddScoped<ApplicationSeeder>();

        // Add realtime notifiers
        services.AddScoped<IMessageSentNotifier, SignalRMessageSentNotifier>();
        services.AddScoped<IUserJoinedChatNotifier, SignalRUserJoinedChatNotifier>();

        return services;
    }
}
