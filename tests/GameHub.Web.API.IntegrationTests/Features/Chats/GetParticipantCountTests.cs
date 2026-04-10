using FluentAssertions;
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Identity;
using GameHub.Contracts.Identity;
using GameHub.Domain.Chats;
using GameHub.Domain.Channels;
using GameHub.Domain.Users;
using GameHub.Infrastructure.Identity.Models;
using GameHub.Web.API.IntegrationTests.Abstractions;
using GameHub.Web.API.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Net;
using System.Net.Http.Json;

namespace GameHub.Web.API.IntegrationTests.Features.Chats;

[Collection(SharedTestCollection.FixtureName)]
public class GetParticipantCountTests(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    private IServiceScope _scope = null!;
    private IApplicationDbContext _context = null!;
    private UserManager<ApplicationUser> _userManager = null!;
    private IIdentityService _identityService = null!;

    protected HttpClient HttpClient => factory.HttpClient;
    protected IApplicationDbContext Context => _context;

    public Task InitializeAsync()
    {
        _scope = factory.Services.CreateScope();
        var services = _scope.ServiceProvider;

        _context = services.GetRequiredService<IApplicationDbContext>();
        _userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        _identityService = services.GetRequiredService<IIdentityService>();

        return Task.CompletedTask;
    }
    public async Task DisposeAsync()
    {
        await factory.ResetDatabaseAsync();
        _scope.Dispose();
    }

    [Fact]
    public async Task GetChatParticipantsCount_ShouldReturn_NotFound_WhenChatId_DoesNotExists()
    {
        AuthenticateClient();
        var response = await HttpClient.GetAsync($"/api/chat/{Guid.NewGuid()}/members/count");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetChatParticipantsCount_ShouldReturn_Ok_WhenChatId_IsValid()
    {

        var chat = await CreateChatWithChannelsAsync();
        await CreateAdminUserAsync();

        var users = await CreateMockUserProfilesAsync();
        users.Should().NotBeNull();

        var jointedAt = DateTimeOffset.UtcNow;

        users.ForEach(user =>
        {
            chat.Join(user.Id, user.Username, jointedAt);
            jointedAt = jointedAt.AddMinutes(5);
        });

        await Context.SaveChangesAsync();

        AuthenticateClient();

        var response = await HttpClient.GetAsync($"/api/chat/{chat.Id}/members/count");
        response.EnsureSuccessStatusCode();

        var count = await response.Content.ReadFromJsonAsync<int>();
        count.Should().Be(users.Count);
    }

    private async Task CreateAdminUserAsync()
    {
        var identityUser = new ApplicationUser
        {
            Id = SystemUsers.AdminUserId,
            Fullname = SystemUsers.AdminName,
            UserName = SystemUsers.AdminUsername,
            Email = SystemUsers.AdminEmail,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var identityResult = await _userManager.CreateAsync(identityUser, SystemUsers.AdminPassword);
        identityResult.Succeeded.Should().BeTrue();

        var userProfile = new UserProfile(
            id: SystemUsers.AdminUserId,
            email: SystemUsers.AdminEmail,
            username: SystemUsers.AdminUsername,
            fullname: SystemUsers.AdminName,
            createdAt: identityUser.CreatedAt);

        Context.UserProfiles.Add(userProfile);
        await Context.SaveChangesAsync();

        var persistedUser = await Context.UserProfiles.FindAsync([SystemUsers.AdminUserId]);
        persistedUser.Should().NotBeNull();
    }

    private async Task<Chat> CreateChatWithChannelsAsync()
    {
        if (!Context.Channels.Any())
        {
            Context.Channels.AddRange(Channel.GetValues());
        }

        var chat = Chat.Create(1, DateTimeOffset.UtcNow).Value;
        Context.Chats.Add(chat);

        await Context.SaveChangesAsync();

        return chat;
    }

    private async Task<UserProfile> CreateUserProfileAsync(
        string email,
        string username,
        string fullname)
    {
        var request = new RegisterUserRequest
        {
            Email = email,
            Username = username,
            Fullname = fullname,
            Password = "Password.01",
            ConfirmPassword = "Password.01"
        };

        var result = await _identityService.RegisterAsync(request);
        result.IsSuccess.Should().BeTrue();

        var userProfile = await Context.UserProfiles.FindAsync([result.Value]);
        userProfile.Should().NotBeNull();

        return userProfile!;
    }

    private async Task<List<UserProfile>> CreateMockUserProfilesAsync()
    {
        var users = new List<UserProfile> {
            await CreateUserProfileAsync("john.doe@mail.com", "john_doe", "John Doe"),
            await CreateUserProfileAsync("jane.doe@mail.com", "jane_doe", "Jane Doe"),
            await CreateUserProfileAsync("alice.smith@mail.com", "alice_smith", "Alice Smith"),
            await CreateUserProfileAsync("bob.johnson@mail.com", "bob_johnson", "Bob Johnson"),
            await CreateUserProfileAsync("charlie.brown@mail.com", "charlie_brown", "Charlie Brown"),
            await CreateUserProfileAsync("david.wilson@mail.com", "david_wilson", "David Wilson"),
            await CreateUserProfileAsync("emma.taylor@mail.com", "emma_taylor", "Emma Taylor"),
            await CreateUserProfileAsync("frank.miller@mail.com", "frank_miller", "Frank Miller"),
            await CreateUserProfileAsync("grace.moore@mail.com", "grace_moore", "Grace Moore"),
            await CreateUserProfileAsync("henry.anderson@mail.com", "henry_anderson", "Henry Anderson"),
            await CreateUserProfileAsync("isabella.thomas@mail.com", "isabella_thomas", "Isabella Thomas"),
            await CreateUserProfileAsync("jack.white@mail.com", "jack_white", "Jack White")
        };

        return users
            .OrderBy(x => x.Username)
            .ThenBy(x => x.Id)
            .ToList();
    }

    private void AuthenticateClient()
    {
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                TestJwtTokenGenerator.Generate(Guid.NewGuid(), "random_mail@mail.com"));
    }

}
