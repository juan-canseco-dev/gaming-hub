using GameHub.Web.UI.Features.Auth.State;
using GameHub.Web.UI.Infrastructure.Options;
using GameHub.Web.UI.Shared.Constants;
using Microsoft.AspNetCore.SignalR.Client;

namespace GameHub.Web.UI.Shared.Extensions;

public static class HubExtensions
{
    public static HubConnection TryInitialize(
        this HubConnection hubConnection,
        JwtAuthenticationStateProvider authProvider,
        ApiSettings settings
    )
    {
        if (hubConnection is null)
        {
            var hubUrl = new Uri(settings.BaseHubUrl + GameHubConstants.SignalR.ChatHub);
            hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl.AbsoluteUri, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(authProvider.Token);
                })
                .WithAutomaticReconnect()
                .Build();
        }
        return hubConnection;
    }
}
