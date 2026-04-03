using GameHub.Web.UI.Features.Auth.State;
using GameHub.Web.UI.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace GameHub.Web.UI.Infrastructure.Http;

public class AuthorizationInterceptor : DelegatingHandler
{
    private readonly JwtAuthenticationStateProvider _authProvider;
    private readonly ApiSettings _apiSettings;

    public AuthorizationInterceptor(
        IOptions<ApiSettings> apiSettingsOptions,
        JwtAuthenticationStateProvider authProvider
    )
    {
        _apiSettings = apiSettingsOptions.Value;
        _authProvider = authProvider;
    }

    protected override HttpResponseMessage Send(
        HttpRequestMessage request, 
        CancellationToken cancellationToken
    )
    {
        return SendAsync(request, cancellationToken).Result;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken
    )
    {
        var baseApiUri= new Uri(_apiSettings.BaseUrl);
        var uri = request.RequestUri;
        var isApiUri = uri is null ? false :baseApiUri.IsBaseOf(uri);

        if (isApiUri)
        { 
            if (!uri!.AbsolutePath.Contains("identity", StringComparison.OrdinalIgnoreCase))
            {
                var token = _authProvider.Token;
                if (!string.IsNullOrWhiteSpace(token)) 
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }
        }
        return base.SendAsync(request, cancellationToken);
    }

}
