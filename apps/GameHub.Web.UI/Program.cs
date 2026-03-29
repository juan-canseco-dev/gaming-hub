using GameHub.Web.UI.Authentication;
using GameHub.Web.UI.Interceptor;
using GameHub.Web.UI.Services;
using GameHub.Web.UI.Services.Interfaces;
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
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            var baseApiUri = new Uri(builder.Configuration["Api:BaseUrl"]!);

            builder.Services.AddScoped(sp => 
                new TokenInterceptor(
                    baseApiUri, 
                    sp.GetRequiredService<JwtAuthenticationStateProvider>()
                )
            );

            builder.Services.AddHttpClient(
                "GameHub.Web.Api",
                client => client.BaseAddress = baseApiUri
            ).AddHttpMessageHandler<TokenInterceptor>();

            builder.Services.AddScoped(
                sp => sp.GetRequiredService<IHttpClientFactory>()
                        .CreateClient("GameHub.Web.Api")
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
