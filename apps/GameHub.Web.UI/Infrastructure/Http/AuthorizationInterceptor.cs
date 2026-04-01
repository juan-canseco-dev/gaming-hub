using GameHub.Web.UI.Features.Auth.State;
using System.Net.Http.Headers;

namespace GameHub.Web.UI.Infrastructure.Http;

public class AuthorizationInterceptor : DelegatingHandler
{
    private readonly Uri _baseApiUri;
    private readonly JwtAuthenticationStateProvider _authProvider;

    public AuthorizationInterceptor(
        Uri baseApiUri, 
        JwtAuthenticationStateProvider authProvider
    )
    {
        _baseApiUri = baseApiUri;
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
        var uri = request.RequestUri;
        var isApiUri = uri is null ? false : _baseApiUri.IsBaseOf(uri);

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
