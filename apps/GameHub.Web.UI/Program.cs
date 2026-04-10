using GameHub.Web.UI.Features.Auth.Services;
using GameHub.Web.UI.Features.Auth.State;
using GameHub.Web.UI.Features.Chats.Services;
using GameHub.Web.UI.Features.Chats.Services.Interfaces;
using GameHub.Web.UI.Features.Channels.Services;
using GameHub.Web.UI.Features.Channels.Services.Interfaces;
using GameHub.Web.UI.Infrastructure.Http;
using GameHub.Web.UI.Infrastructure.Options;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

namespace GameHub.Web.UI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            builder.Configuration
               .AddJsonFile("appsettings.json", optional: false)
               .AddJsonFile($"appsettings.{builder.HostEnvironment.Environment}.json", optional: true);

            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            builder.Services.Configure<ApiSettings>(
                builder.Configuration.GetSection(ApiSettings.SectionName)
            );

            var baseApiUri = new Uri(builder.Configuration["ApiSettings:BaseUrl"]!);

            builder.Services.AddScoped<AuthorizationInterceptor>();
            builder.Services.AddScoped<UnauthorizedInterceptor>();

            builder.Services.AddHttpClient(
                "GameHub.Web.API",
                client => client.BaseAddress = baseApiUri
            ).AddHttpMessageHandler<AuthorizationInterceptor>()
            .AddHttpMessageHandler<UnauthorizedInterceptor>();

            builder.Services.AddScoped(
                sp => sp.GetRequiredService<IHttpClientFactory>()
                        .CreateClient("GameHub.Web.API")
            );


            builder.Services.AddMudServices();
            builder.Services.AddLocalStorageServices();

            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IChannelsService, ChannelsService>();
            builder.Services.AddScoped<IChatService, ChatService>();

            builder.Services.AddScoped<JwtAuthenticationStateProvider>();
            builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());
            builder.Services.AddAuthorizationCore();


            await builder.Build().RunAsync();
        }
    }
}
