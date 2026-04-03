using FluentAssertions;
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Identity;
using GameHub.Contracts.Chats;
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

namespace GameHub.WebAPI.IntegrationTests.Features.Chats;

[Collection(SharedTestCollection.FixtureName)]
public class GetChatByIdTests(CustomWebApplicationFactory factory) : IAsyncLifetime
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
    public async Task GetChatById_ShouldReturnNotFound_WhenUserHasNoChats()
    {
        // Arrange
        var user = await CreateUserProfileAsync(
            "john.doe@mail.com",
            "john_doe",
            "John Doe");

        AuthenticateClient(user.Id);

        // Act
        var response = await HttpClient.GetAsync($"/api/chats/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetChatById_ShouldReturnNotFound_WhenUserIsNotParticipant()
    {
        // Arrange
        await CreateAdminUserAsync();

        var owner = await CreateUserProfileAsync(
            "owner@mail.com",
            "owner_user",
            "Owner User");

        var anotherUser = await CreateUserProfileAsync(
            "another@mail.com",
            "another_user",
            "Another User");

        AuthenticateClient(anotherUser.Id);

        var chat = await CreateChatWithChannelsAsync();
        chat.Join(owner.Id, owner.Username, DateTimeOffset.UtcNow);

        await Context.SaveChangesAsync();

        // Act
        var response = await HttpClient.GetAsync($"/api/chats/{chat.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetChatById_ShouldReturnRequestedChat_WhenUserIsParticipant()
    {
        // Arrange
        await CreateAdminUserAsync();

        var user = await CreateUserProfileAsync(
            "john.doe@mail.com",
            "john_doe",
            "John Doe");

        AuthenticateClient(user.Id);

        var chat = await CreateChatWithChannelsAsync();

        chat.Join(user.Id, user.Username, DateTimeOffset.UtcNow);

        await Context.SaveChangesAsync();

        // Act
        var response = await HttpClient.GetAsync($"/api/chats/{chat.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ChatDto>();
        result.Should().NotBeNull();

        result!.Id.Should().Be(chat.Id);
        result.ChannelId.Should().Be(chat.ChannelId);
        result.ParticipantsCount.Should().Be(1);
    }

    [Fact]
    public async Task GetChatById_ShouldReturnRequestedChat_WhenUserHasMoreThanOneChat()
    {
        // Arrange
        await CreateAdminUserAsync();

        var user = await CreateUserProfileAsync(
            "john.doe@mail.com",
            "john_doe",
            "John Doe");

        AuthenticateClient(user.Id);

        var olderChat = await CreateChatWithChannelsAsync(DateTimeOffset.UtcNow.AddMinutes(-10));
        var requestedChat = await CreateChatWithChannelsAsync(DateTimeOffset.UtcNow);

        olderChat.Join(user.Id, user.Username, olderChat.CreatedAt);
        requestedChat.Join(user.Id, user.Username, requestedChat.CreatedAt);

        await Context.SaveChangesAsync();

        // Act
        var response = await HttpClient.GetAsync($"/api/chats/{requestedChat.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ChatDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(requestedChat.Id);
    }

    [Fact]
    public async Task GetChatById_ShouldReturnUnreadMessagesCount()
    {
        // Arrange
        await CreateAdminUserAsync();

        var user = await CreateUserProfileAsync(
            "john.doe@mail.com",
            "john_doe",
            "John Doe");

        var otherUser = await CreateUserProfileAsync(
            "jane.doe@mail.com",
            "jane_doe",
            "Jane Doe");

        AuthenticateClient(user.Id);

        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        var chat = await CreateChatWithChannelsAsync(createdAt);

        chat.Join(user.Id, user.Username, createdAt);
        chat.Join(otherUser.Id, otherUser.Username, createdAt);

        var previewService = new MessagePreviewService();

        chat.AddMessage(otherUser.Id, "old message", createdAt.AddMinutes(1), previewService);

        await Context.SaveChangesAsync();

        var member = Context.ChatMembers
            .First(x => x.ChatId == chat.Id && x.UserId == user.Id);

        member.ReadUpTo(createdAt.AddMinutes(2));

        chat.AddMessage(otherUser.Id, "new message", createdAt.AddMinutes(3), previewService);

        await Context.SaveChangesAsync();

        // Act
        var response = await HttpClient.GetAsync($"/api/chats/{chat.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ChatDto>();

        result.Should().NotBeNull();
        result!.UnreadCount.Should().Be(1);
    }

    [Fact]
    public async Task GetChatById_ShouldReturnUnreadMessagesCount_WhenLastReadAtIsNull()
    {
        // Arrange
        await CreateAdminUserAsync();

        var user = await CreateUserProfileAsync(
            "john.doe@mail.com",
            "john_doe",
            "John Doe");

        var otherUser = await CreateUserProfileAsync(
            "jane.doe@mail.com",
            "jane_doe",
            "Jane Doe");

        AuthenticateClient(user.Id);

        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        var chat = await CreateChatWithChannelsAsync(createdAt);

        chat.Join(user.Id, user.Username, createdAt);
        chat.Join(otherUser.Id, otherUser.Username, createdAt);

        var previewService = new MessagePreviewService();

        chat.AddMessage(otherUser.Id, "message 1", createdAt.AddMinutes(1), previewService);
        chat.AddMessage(otherUser.Id, "message 2", createdAt.AddMinutes(2), previewService);
        chat.AddMessage(user.Id, "mine", createdAt.AddMinutes(3), previewService);

        await Context.SaveChangesAsync();

        // Act
        var response = await HttpClient.GetAsync($"/api/chats/{chat.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ChatDto>();

        result.Should().NotBeNull();
        result!.UnreadCount.Should().Be(4);
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

    private async Task<Chat> CreateChatWithChannelsAsync(DateTimeOffset? createdAt = null)
    {
        if (!Context.Channels.Any())
        {
            Context.Channels.AddRange(Channel.GetValues());
        }

        var chat = Chat.Create(1, createdAt ?? DateTimeOffset.UtcNow).Value;
        Context.Chats.Add(chat);

        await Context.SaveChangesAsync();

        return chat;
    }

    private void AuthenticateClient(Guid userId)
    {
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                TestJwtTokenGenerator.Generate(userId, "random_mail@mail.com"));
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
    }
}