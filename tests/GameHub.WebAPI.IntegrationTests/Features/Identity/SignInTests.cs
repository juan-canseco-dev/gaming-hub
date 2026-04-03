using FluentAssertions;
using GameHub.Application.Abstractions.Identity;
using GameHub.Contracts.Identity;
using GameHub.Abstractions.Primitives;
using GameHub.Infrastructure.Identity.Models;
using GameHub.WebAPI.IntegrationTests.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace GameHub.WebAPI.IntegrationTests.Features.Identity;

[Collection(SharedTestCollection.FixtureName)]
public class SignInTests(CustomWebApplicationFactory factory) : IAsyncLifetime
{

    protected IServiceScope Scope { get; private set; } = null!;
    protected UserManager<ApplicationUser> UserManager { get; private set; } = null!;

    public Task InitializeAsync()
    {
        Scope = factory.Services.CreateScope();
        UserManager = Scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return Task.CompletedTask;
    }


    public async Task DisposeAsync()
    {
        await factory.ResetDatabaseAsync();
        Scope.Dispose();
    }


    [Fact]
    public async Task Login_Should_Return_Jwt_When_Credentials_Are_Valid()
    {
        var email = "login@test.com";
        var password = "AdminPassword.01";
        var username = "login_username";

        var identityUser = new ApplicationUser
        {
            Email = email,
            UserName = email,
            Fullname = username,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await UserManager.CreateAsync(identityUser, password);


        var request = new GetTokenRequest {
            Email = email,
            Password = password
        };

        var httpResponse = await factory.HttpClient.PostAsJsonAsync("api/identity/auth", request);
        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var jwtResponse = await httpResponse.Content.ReadAsStringAsync();

        jwtResponse.Should().NotBeNull();
    }


    [Fact]
    public async Task Login_Should_Fail_When_Password_Is_Invalid()
    {
        var email = "login@test.com";
        var username = "login_name";
        var password = "AdminPassword.01";
        var invalidPassword = "invalid.password.01";

        var identityUser = new ApplicationUser
        {
            Email = email,
            UserName = username,
            Fullname = "Test User",
            CreatedAt = DateTimeOffset.UtcNow
        };

        await UserManager.CreateAsync(identityUser, password);


        var request = new GetTokenRequest
        {
            Email = email,
            Password = invalidPassword,
        };

        var httpResponse = await factory.HttpClient.PostAsJsonAsync("api/identity/auth", request);
        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResult = await httpResponse.Content.ReadFromJsonAsync<Error>();
        errorResult.Should().NotBeNull();
        errorResult.Should().Be(IdentityErrors.InvalidCredentials);
    }
}
