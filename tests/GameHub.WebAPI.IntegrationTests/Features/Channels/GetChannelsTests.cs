using FluentAssertions;
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Identity;
using GameHub.Contracts.Channels;
using GameHub.Contracts.Identity;
using GameHub.Domain.Chats;
using GameHub.Domain.Users;
using GameHub.Infrastructure.Identity.Models;
using GameHub.WebAPI.IntegrationTests.Abstractions;
using GameHub.WebAPI.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace GameHub.WebAPI.IntegrationTests.Features.Channels;

[Collection(SharedTestCollection.FixtureName)]
public class GetChannelsTests(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    private IServiceScope _scope = null!;
    private IApplicationDbContext _context = null!;
    private IIdentityService _identityService = null!;
    private UserManager<ApplicationUser> _userManager = null!;

    protected HttpClient HttpClient => factory.HttpClient;
    protected IApplicationDbContext Context => _context;

    public Task InitializeAsync()
    {
        _scope = factory.Services.CreateScope();
        var services = _scope.ServiceProvider;

        _context = services.GetRequiredService<IApplicationDbContext>();
        _identityService = services.GetRequiredService<IIdentityService>();
        _userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await factory.ResetDatabaseAsync();
        _scope.Dispose();
    }

    [Fact]
    public async Task GetChannels_Should_Return_IsJoined_True_When_User_Is_Member()
    {
        // Arrange
        await SetupAdminUser();
        var chat = await SetupChatAndChannels();
        var user = await SetupUser();

        chat.Join(user.Id, user.Username, user.CreatedAt);
        await Context.SaveChangesAsync();

        Authenticate(user.Id, user.Email);

        // Act
        var response = await HttpClient.GetAsync("/api/channels");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var channels = await response.Content.ReadFromJsonAsync<List<ChannelDto>>();
        channels.Should().NotBeNull();

        var channel = channels!.Single();

        channel.IsJoined.Should().BeTrue(); 
        channel.ParticipantsCount.Should().Be(1);
    }

    [Fact]
    public async Task GetChannels_Should_Return_IsJoined_False_When_User_Is_Not_Member()
    {
        // Arrange
        var chat = await SetupChatAndChannels();
        var user = await SetupUser();

        await Context.SaveChangesAsync();

        Authenticate(user.Id, user.Email);

        // Act
        var response = await HttpClient.GetAsync("/api/channels");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var channels = await response.Content.ReadFromJsonAsync<List<ChannelDto>>();
        channels.Should().NotBeNull();

        var channel = channels!.Single();

        channel.IsJoined.Should().BeFalse(); 
        channel.ParticipantsCount.Should().Be(0);
    }

    [Fact]
    public async Task GetChannels_Should_Return_Channels_Ordered_By_ChannelId()
    {
        // Arrange
        await SeedChannels();

        var chat1 = Chat.Create(2, DateTimeOffset.UtcNow).Value;
        var chat2 = Chat.Create(1, DateTimeOffset.UtcNow).Value;

        Context.Chats.AddRange(chat1, chat2);
        await Context.SaveChangesAsync();

        var user = await SetupUser();
        Authenticate(user.Id, user.Email);

        // Act
        var response = await HttpClient.GetAsync("/api/channels");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var channels = await response.Content.ReadFromJsonAsync<List<ChannelDto>>();
        channels.Should().NotBeNull();

        channels!.Count.Should().Be(2);
        channels[0].Id.Should().Be(1);
        channels[1].Id.Should().Be(2);
    }

    private async Task SeedChannels()
    {
        if (!Context.Channels.Any())
        {
            Context.Channels.AddRange(Channel.GetValues());
            await Context.SaveChangesAsync();
        }
    }

    private async Task<Chat> SetupChatAndChannels()
    {
        await SeedChannels();

        var chat = Chat.Create(1, DateTimeOffset.UtcNow).Value;
        Context.Chats.Add(chat);

        await Context.SaveChangesAsync();
        return chat;
    }

    private async Task<UserProfile> SetupUser()
    {
        var request = new RegisterUserRequest
        {
            Fullname = "John Doe",
            Email = "john_doe@mail.com",
            Username = "john_doe",
            Password = "Password.01",
            ConfirmPassword = "Password.01"
        };

        var result = await _identityService.RegisterAsync(request);
        result.IsSuccess.Should().BeTrue();

        var user = await Context.UserProfiles.FindAsync([result.Value]);
        user.Should().NotBeNull();

        return user!;
    }

    private async Task SetupAdminUser()
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
        identityResult.Should().NotBeNull();

        var userProfile = new UserProfile(
            id: SystemUsers.AdminUserId,
            email: SystemUsers.AdminEmail,
            username: SystemUsers.AdminUsername,
            fullname: SystemUsers.AdminName,
            createdAt: identityUser.CreatedAt
        );

        Context.UserProfiles.Add(userProfile);

        await Context.SaveChangesAsync();

        var userExists = await Context.UserProfiles.FindAsync([SystemUsers.AdminUserId]);
        userExists.Should().NotBeNull();
    }

    private void Authenticate(Guid userId, string email)
    {
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                TestJwtTokenGenerator.Generate(userId, email));
    }
}
