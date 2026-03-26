using FluentAssertions;
using GameHub.Abstractions.Primitives;
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Identity;
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
public class GetUnreadMessagesCountTests(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    private IServiceScope _scope = null!;
    private IApplicationDbContext _context = null!;
    private UserManager<ApplicationUser> _userManager = null!;
    private IIdentityService _identityService = null!;
    private MessagePreviewService _previewService = null!;

    protected HttpClient HttpClient => factory.HttpClient;
    protected IApplicationDbContext Context => _context;

    public Task InitializeAsync()
    {
        _scope = factory.Services.CreateScope();
        var services = _scope.ServiceProvider;

        _context = services.GetRequiredService<IApplicationDbContext>();
        _userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        _identityService = services.GetRequiredService<IIdentityService>();

        _previewService = services.GetRequiredService<MessagePreviewService>();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await factory.ResetDatabaseAsync();
        _scope.Dispose();
    }


    [Fact]
    public async Task GetUnreadMessagesCount_ShouldReturnNotFound_WhenChatDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var chatId = Guid.NewGuid();

        AuthenticateClient(userId);

        // Act
        var response = await HttpClient.GetAsync($"/api/chats/{chatId}/messages/unread/count");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var error = await response.Content.ReadFromJsonAsync<Error>();
        error.Should().NotBeNull();
        error!.Code.Should().Be(ChatErrors.ChatGroupNotFound(chatId).Code);
    }

    [Fact]
    public async Task GetUnreadMessagesCount_ShouldReturnBadRequest_WhenUserIsNotParticipant()
    {
        // Arrange
        var user = await CreateUserProfileAsync(
            "john.doe@mail.com",
            "john_doe",
            "John Doe");

        AuthenticateClient(user.Id);

        var chat = await CreateChatWithChannelsAsync();

        // Act
        var response = await HttpClient.GetAsync($"/api/chats/{chat.Id}/messages/unread/count");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await response.Content.ReadFromJsonAsync<Error>();
        error.Should().NotBeNull();
        error!.Code.Should().Be(ChatErrors.NotParticipant(user.Id).Code);
    }

    [Fact]
    public async Task GetUnreadMessagesCount_ShouldReturnZero_WhenChatHasNoUnreadMessages()
    {
        // Arrange
        await CreateAdminUserAsync();

        var user = await CreateUserProfileAsync(
            "john.doe@mail.com",
            "john_doe",
            "John Doe");

        AuthenticateClient(user.Id);

        var joinedAt = DateTimeOffset.UtcNow.AddMinutes(-20);
        var chat = await CreateChatWithChannelsAsync(joinedAt);

        chat.Join(user.Id, user.Username, joinedAt);
        await Context.SaveChangesAsync();

        // Act
        var response = await HttpClient.GetAsync($"/api/chats/{chat.Id}/messages/unread/count");

        // Assert
        response.EnsureSuccessStatusCode();

        var unreadCount = await response.Content.ReadFromJsonAsync<int>();
        unreadCount.Should().Be(0);
    }

    [Fact]
    public async Task GetUnreadMessagesCount_ShouldReturnCorrectCount_ForRequestedChatOnly()
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

        var baseTime = DateTimeOffset.UtcNow.AddHours(-1);

        var targetChat = await CreateChatWithChannelsAsync(baseTime);
        var otherChat = await CreateChatWithChannelsAsync(baseTime.AddMinutes(1));

        targetChat.Join(user.Id, user.Username, baseTime);
        targetChat.Join(otherUser.Id, otherUser.Username, baseTime);

        otherChat.Join(user.Id, user.Username, baseTime);
        otherChat.Join(otherUser.Id, otherUser.Username, baseTime);

        await Context.SaveChangesAsync();

        var member = Context.ChatMembers.First(x => x.ChatId == targetChat.Id && x.UserId == user.Id);
        member.ReadUpTo(baseTime.AddMinutes(10));

        var otherMember = Context.ChatMembers.First(x => x.ChatId == otherChat.Id && x.UserId == user.Id);
        otherMember.ReadUpTo(baseTime.AddMinutes(10));

        await Context.SaveChangesAsync();

        targetChat.AddMessage(otherUser.Id, "Read message", baseTime.AddMinutes(9), _previewService);
        targetChat.AddMessage(otherUser.Id, "Unread message 1", baseTime.AddMinutes(11), _previewService);
        targetChat.AddMessage(otherUser.Id, "Unread message 2", baseTime.AddMinutes(12), _previewService);

        otherChat.AddMessage(otherUser.Id, "Message in other chat", baseTime.AddMinutes(13), _previewService);

        await Context.SaveChangesAsync();

        // Act
        var response = await HttpClient.GetAsync($"/api/chats/{targetChat.Id}/messages/unread/count");

        // Assert
        response.EnsureSuccessStatusCode();

        var unreadCount = await response.Content.ReadFromJsonAsync<int>();
        unreadCount.Should().Be(2);
    }

    [Fact]
    public async Task GetUnreadMessagesCount_ShouldExcludeMessagesSentByAuthenticatedUser()
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

        var baseTime = DateTimeOffset.UtcNow.AddHours(-1);
        var chat = await CreateChatWithChannelsAsync(baseTime);

        chat.Join(user.Id, user.Username, baseTime);
        chat.Join(otherUser.Id, otherUser.Username, baseTime);

        await Context.SaveChangesAsync();

        var member = Context.ChatMembers.First(x => x.ChatId == chat.Id && x.UserId == user.Id);
        member.ReadUpTo(baseTime.AddMinutes(10));
        await Context.SaveChangesAsync();

        chat.AddMessage(user.Id, "My own message", baseTime.AddMinutes(11), _previewService);
        chat.AddMessage(otherUser.Id, "Unread from other user", baseTime.AddMinutes(12), _previewService);
        chat.AddMessage(otherUser.Id, "Unread from other user 2", baseTime.AddMinutes(13), _previewService);

        await Context.SaveChangesAsync();

        // Act
        var response = await HttpClient.GetAsync($"/api/chats/{chat.Id}/messages/unread/count");

        // Assert
        response.EnsureSuccessStatusCode();

        var unreadCount = await response.Content.ReadFromJsonAsync<int>();
        unreadCount.Should().Be(2);
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

        var persistedUser = await Context.UserProfiles.FindAsync([SystemUsers.AdminUserId]);
        persistedUser.Should().NotBeNull();
    }
}