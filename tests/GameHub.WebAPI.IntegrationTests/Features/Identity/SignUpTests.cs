using FluentAssertions;
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Identity;
using GameHub.Application.Contracts.Identity;
using GameHub.Domain.Abstractions;
using GameHub.Infrastructure.Identity.Models;
using GameHub.WebAPI.IntegrationTests.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace GameHub.WebAPI.IntegrationTests.Features.Identity;

[Collection(SharedTestCollection.FixtureName)]
public class SignUpTests(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    protected IServiceScope Scope { get; private set; } = null!;
    protected UserManager<ApplicationUser> UserManager { get; private set; } = null!;
    protected IApplicationDbContext DbContext { get; private set; } = null!;
    protected IIdentityService Service { get; private set; } = null!;

    public Task InitializeAsync()
    {
        Scope = factory.Services.CreateScope();
        UserManager = Scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        DbContext = Scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        Service = Scope.ServiceProvider.GetRequiredService<IIdentityService>();
        return Task.CompletedTask;
    }


    public async Task DisposeAsync()
    {
        await factory.ResetDatabaseAsync();
        Scope.Dispose();
    }

    [Fact]
    public async Task Register_Should_Be_Successful_When_Request_Is_Valid()
    {
        var request = new RegisterUserRequest
        {
            Fullname = "Super Admin",
            Username = "super_admin",
            Email = "super.admin@gmail.com",
            Password = "SupperPassword.01",
            ConfirmPassword = "SupperPassword.01"
        };

        var httpResponse = await factory.HttpClient.PostAsJsonAsync("api/identity/auth/register", request);
        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var userId = await httpResponse.Content.ReadFromJsonAsync<Guid>(); 
        

        var identityUser = await UserManager.FindByEmailAsync(request.Email);
        var domainUser = await DbContext.UserProfiles.FindAsync([userId]);

        identityUser.Should().NotBeNull();
        domainUser.Should().NotBeNull();

        identityUser.Id.Should().Be(userId);
        identityUser.Email.Should().Be(request.Email);
        identityUser.UserName.Should().Be(request.Username);
        identityUser.Fullname.Should().Be(request.Fullname);

        domainUser.Id.Should().Be(identityUser.Id);
        domainUser.Fullname.Should().Be(request.Fullname);
        domainUser.Username.Should().Be(request.Username);
        domainUser.Email.Should().Be(request.Email);
        domainUser.CreatedAt.Should().Be(identityUser.CreatedAt);
    }

    [Fact]
    public async Task Register_Should_Fail_When_Email_Already_Exists()
    {
        var userWithSameEmail = new RegisterUserRequest
        {
            Fullname = "Super Admin",
            Email = "super.admin@gmail.com",
            Username = "super_admin_1",
            Password = "SupperPassword.01",
            ConfirmPassword = "SupperPassword.01"
        };

        await Service.RegisterAsync(userWithSameEmail);

        var request = new RegisterUserRequest
        {
            Fullname = "Super Admin 2",
            Username = "super_admin_2",
            Email = "super.admin@gmail.com",
            Password = "SupperPassword.02",
            ConfirmPassword = "SupperPassword.02"
        };


        var httpResponse = await factory.HttpClient.PostAsJsonAsync("api/identity/auth/register", request);
        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResult = await httpResponse.Content.ReadFromJsonAsync<Error>();
        errorResult.Should().Be(IdentityErrors.EmailAlreadyExists);
    }


    [Fact]
    public async Task Register_Should_Fail_When_Username_Already_Exists()
    {
        var userWithSameEmail = new RegisterUserRequest
        {
            Fullname = "Super Admin",
            Email = "super.admin@gmail.com",
            Username = "super_admin",
            Password = "SupperPassword.01",
            ConfirmPassword = "SupperPassword.01"
        };

        await Service.RegisterAsync(userWithSameEmail);

        var request = new RegisterUserRequest
        {
            Fullname = "Super Admin 2",
            Username = "super_admin",
            Email = "super.admin2@gmail.com",
            Password = "SupperPassword.02",
            ConfirmPassword = "SupperPassword.02"
        };


        var httpResponse = await factory.HttpClient.PostAsJsonAsync("api/identity/auth/register", request);
        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResult = await httpResponse.Content.ReadFromJsonAsync<Error>();
        errorResult.Should().Be(IdentityErrors.UsernameAlreadyExists);
    }


}
