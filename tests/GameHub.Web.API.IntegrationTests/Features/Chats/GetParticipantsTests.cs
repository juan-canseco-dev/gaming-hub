using FluentAssertions;
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Identity;
using GameHub.Contracts.Identity;
using GameHub.Contracts.Profile;
using GameHub.Domain.Chats;
using GameHub.Domain.Channels;
using GameHub.Domain.Users;
using GameHub.Infrastructure.Identity.Models;
using GameHub.Web.API.IntegrationTests.Abstractions;
using GameHub.Web.API.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GameHub.Abstractions.Pagination;

namespace GameHub.Web.API.IntegrationTests.Features.Chats;

[Collection(SharedTestCollection.FixtureName)]
public class GetParticipantsTests(CustomWebApplicationFactory factory) : IAsyncLifetime
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

    [Fact]
    public async Task GetParticipants_ShouldReturnAllParticipantsAcrossCursorPages()
    {
        const int limit = 6;

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

        var allParticipants = await GetAllPagesAsync(chat.Id, limit);

        allParticipants.Count.Should().Be(12);

        UsersShouldMatch(users, allParticipants);
    }

    public async Task DisposeAsync()
    {
        await factory.ResetDatabaseAsync();
        _scope.Dispose();
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

    private async Task<List<UserDto>> GetAllPagesAsync(Guid chatId, int limit)
    {
        var allParticipants = new List<UserDto>();
        string? cursor = null;

        do
        {
            var page = await GetParticipantsPageAsync(chatId, limit, cursor);

            allParticipants.AddRange(page.Items);
            cursor = page.Next;
        }
        while (cursor is not null);

        return allParticipants
            .OrderBy(x => x.Username)
            .ThenBy(x => x.Id)
            .ToList();
    }

    private async Task<CursorPage<UserDto>> GetParticipantsPageAsync(Guid chatId, int limit, string? cursor = null)
    {
        var uri = cursor is null
            ? $"/api/chat/{chatId}/members?limit={limit}"
            : $"/api/chat/{chatId}/members?limit={limit}&cursor={cursor}";

        var response = await HttpClient.GetAsync(uri);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CursorPage<UserDto>>();
        result.Should().NotBeNull();

        return result!;
    }

    private static void UsersShouldMatch(
     IReadOnlyList<UserProfile> expectedUsers,
     IReadOnlyList<UserDto> actualUsers)
    {
        actualUsers.Should().HaveCount(expectedUsers.Count);

        for (var i = 0; i < expectedUsers.Count; i++)
        {
            var expected = expectedUsers[i];
            var actual = actualUsers[i];

            actual.Id.Should().Be(expected.Id);
            actual.Username.Should().Be(expected.Username);
            actual.Email.Should().Be(expected.Email);
            actual.Fullname.Should().Be(expected.Fullname);
        }
    }
}
