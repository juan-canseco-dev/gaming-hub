using GameHub.Abstractions.Primitives;
using GameHub.Contracts.Identity;
using GameHub.Web.UI.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net;
using System.Net.Http.Json;

namespace GameHub.Web.UI.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly JwtAuthenticationStateProvider _authStateProvider;

    public AuthService(HttpClient httpClient, AuthenticationStateProvider authStateProvider)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _authStateProvider = (JwtAuthenticationStateProvider)authStateProvider ?? throw new ArgumentNullException(nameof(authStateProvider));
    }

    public async Task<Result<UserDetails>> LoginAsync(GetTokenRequest request)
    {
        var result = await _httpClient.PostAsJsonAsync("identity/auth", request);

        if (result.IsSuccessStatusCode)
        {
            var tokenResponse = await result.Content.ReadFromJsonAsync<GetTokenResponse>();
            await _authStateProvider.LoginAsync(tokenResponse!.Token);
            var userDetails = await _authStateProvider.GetAuthenticatedUserDetailsAsync();
            return Result.Success(userDetails!);
        }

        if (result.StatusCode == HttpStatusCode.BadRequest)
        {
            var error = await result.Content.ReadFromJsonAsync<Error>();
            return Result.Failure<UserDetails>(error!);
        }

        return Result.Failure<UserDetails>(Error.InternalServerError);
    }

    public async Task<Result> RegisterAsync(RegisterUserRequest request)
    {
        var result = await _httpClient.PostAsJsonAsync("identity/auth/register", request);

        if (result.IsSuccessStatusCode)
        {
            return Result.Success();
        }

        if (result.StatusCode == HttpStatusCode.BadRequest)
        {
            var error = await result.Content.ReadFromJsonAsync<Error>();
            return Result.Failure(error!);
        }

        return Result.Failure(Error.InternalServerError);
    }
}
