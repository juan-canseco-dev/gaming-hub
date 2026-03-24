using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace GameHub.Web.UI.Authentication;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private ILocalStorageService _localStorage;
    private HttpClient _httpClient;

    public JwtAuthenticationStateProvider(ILocalStorageService localStorage, HttpClient httpClient)
    {
        _localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = _localStorage.GetItem<string>("token");

        var identity = string.IsNullOrEmpty(token) ? new ClaimsIdentity() : new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt");

        _httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(token) ? null : new AuthenticationHeaderValue("Bearer", token);

        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task<UserDetails?> GetAuthenticatedUserDetailsAsync()
    {
        var authState = await GetAuthenticationStateAsync();

        if (!authState.User.Claims.Any()) return null;

        var claims = authState.User.Claims;

        var id = claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub)?.Value;
        var userName = claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.PreferredUsername)?.Value;
        var email = claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Email)?.Value;
        var fullname = claims.FirstOrDefault(x => x.Type == ClaimTypes.GivenName)?.Value;

        return new UserDetails
        {
            Id = Guid.Parse(id!),
            Email = email!,
            UserName = userName!,
            Fullname = fullname!
        };
    }

    public async Task<bool> IsLoggedInAsync()
    {
        var authState = await GetAuthenticationStateAsync();
        return authState.User.Claims.Any();
    }

    public async Task LoginAsync(string token)
    {
        _localStorage.SetItem("token", token);
        var identity = new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt");
        var user = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public async Task LogOutAsync()
    {
        _localStorage.RemoveItem("token");
        var identity = new ClaimsIdentity();
        var user = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var handler = new JsonWebTokenHandler();
        var token = handler.ReadJsonWebToken(jwt);
        return token.Claims;
    }
}
