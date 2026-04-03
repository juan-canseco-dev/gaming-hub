using GameHub.Web.UI.Features.Auth.State;
using Microsoft.AspNetCore.Components;

namespace GameHub.Web.UI.Infrastructure.Http;

public class UnauthorizedInterceptor : DelegatingHandler
{
    private JwtAuthenticationStateProvider _authProvider;
    private NavigationManager _navManager;

    public UnauthorizedInterceptor(
        JwtAuthenticationStateProvider authProvider, 
        NavigationManager navManager
    )
    {
        _authProvider = authProvider;
        _navManager = navManager;
    }

    protected override HttpResponseMessage Send(
        HttpRequestMessage request, 
        CancellationToken cancellationToken
    )
    {
        return SendAsync(request, cancellationToken).Result;
    }

    protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await _authProvider.LogOutAsync();
            _navManager.NavigateTo("/auth/login?sessionExpired=true", forceLoad: true);
        }

        return response;
    }

}
