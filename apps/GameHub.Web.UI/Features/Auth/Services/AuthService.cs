using GameHub.Abstractions.Primitives;
using GameHub.Contracts.Identity;
using GameHub.Web.UI.Features.Auth.Models;
using GameHub.Web.UI.Features.Auth.State;
using Microsoft.AspNetCore.Components.Authorization;
using GameHub.Web.UI.Infrastructure.Http;
using System.Net.Http.Json;

namespace GameHub.Web.UI.Features.Auth.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly JwtAuthenticationStateProvider _authStateProvider;

    public AuthService(HttpClient httpClient, AuthenticationStateProvider authStateProvider)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _authStateProvider = (JwtAuthenticationStateProvider)authStateProvider ?? throw new ArgumentNullException(nameof(authStateProvider));
    }

    public async Task<Result<UserDetails>> LoginAsync(GetTokenRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.PostAsJsonAsync("identity/auth", request, cancellationToken);

        if (result.IsSuccessStatusCode)
        {
            var tokenResponse = await result.Content.ReadFromJsonAsync<GetTokenResponse>(cancellationToken: cancellationToken);
            await _authStateProvider.LoginAsync(tokenResponse!.Token);
            var userDetails = await _authStateProvider.GetAuthenticatedUserDetailsAsync();
            return Result.Success(userDetails!);
        }

        return Result.Failure<UserDetails>(await result.ReadProblemAsync());
    }

    public async Task<Result> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.PostAsJsonAsync("identity/auth/register", request, cancellationToken);

        if (result.IsSuccessStatusCode)
        {
            return Result.Success();
        }

        return Result.Failure(await result.ReadProblemAsync());
    }
}
