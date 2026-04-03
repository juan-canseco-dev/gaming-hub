using FluentAssertions;
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
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace GameHub.WebAPI.IntegrationTests.Features.Chats;

[Collection(SharedTestCollection.FixtureName)]
public class GetTotalUnreadMessagesCountTests(CustomWebApplicationFactory factory) : IAsyncLifetime
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
    public async Task GetTotalUnreadMessagesCount_ShouldReturnZero_WhenUserHasNoUnreadMessages()
    {
        // Arrange
        await CreateAdminUserAsync();

        var user = await CreateUserProfileAsync(
            "john.doe@mail.com",
            "john_doe",
            "John Doe");

        AuthenticateClient(user.Id);

        var chat = await CreateChatWithChannelsAsync();
        var joinedAt = DateTimeOffset.UtcNow;

        chat.Join(user.Id, user.Username, joinedAt);

        var member = chat.Members.First(x => x.ChatId == chat.Id && x.UserId == user.Id);
        member.ReadUpTo(joinedAt);

        await Context.SaveChangesAsync();

        // Act
        var response = await HttpClient.GetAsync("/api/chats/unread-count");

        // Assert
        response.EnsureSuccessStatusCode();

        var unreadCount = await response.Content.ReadFromJsonAsync<int>();
        unreadCount.Should().Be(0);
    }

    [Fact]
    public async Task GetTotalUnreadMessagesCount_ShouldReturnCorrectCount_WhenUserHasUnreadMessagesAcrossChats()
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

        var chat1 = await CreateChatWithChannelsAsync(baseTime);
        var chat2 = await CreateChatWithChannelsAsync(baseTime.AddMinutes(1));

        chat1.Join(user.Id, user.Username, baseTime);
        chat2.Join(user.Id, user.Username, baseTime);

        chat1.Join(otherUser.Id, otherUser.Username, baseTime);
        chat2.Join(otherUser.Id, otherUser.Username, baseTime);

        await Context.SaveChangesAsync();

        var member1 = Context.ChatMembers.First(x => x.ChatId == chat1.Id && x.UserId == user.Id);
        var member2 = Context.ChatMembers.First(x => x.ChatId == chat2.Id && x.UserId == user.Id);

        member1.ReadUpTo(baseTime.AddMinutes(10));
        member2.ReadUpTo(baseTime.AddMinutes(20));

        await Context.SaveChangesAsync();

        chat1.AddMessage(otherUser.Id, "Unread message 1", baseTime.AddMinutes(11), _previewService);
        chat1.AddMessage(otherUser.Id, "Unread message 2", baseTime.AddMinutes(12), _previewService);
        chat2.AddMessage(otherUser.Id, "Read message", baseTime.AddMinutes(19), _previewService);
        chat2.AddMessage(otherUser.Id, "Unread message 3", baseTime.AddMinutes(21), _previewService);

        await Context.SaveChangesAsync();

        // Act
        var response = await HttpClient.GetAsync("/api/chats/unread-count");

        // Assert
        response.EnsureSuccessStatusCode();

        var unreadCount = await response.Content.ReadFromJsonAsync<int>();
        unreadCount.Should().Be(3);
    }

    [Fact]
    public async Task GetTotalUnreadMessagesCount_ShouldExcludeMessagesSentByAuthenticatedUser()
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
        chat.AddMessage(otherUser.Id,"Unread from other user", baseTime.AddMinutes(12), _previewService);

        await Context.SaveChangesAsync();

        // Act
        var response = await HttpClient.GetAsync("/api/chats/unread-count");

        // Assert
        response.EnsureSuccessStatusCode();

        var unreadCount = await response.Content.ReadFromJsonAsync<int>();
        unreadCount.Should().Be(1);
    }

    [Fact]
    public async Task GetTotalUnreadMessagesCount_ShouldExcludeMessagesFromChatsWhereUserIsNotMember()
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

        var joinedChat = await CreateChatWithChannelsAsync(baseTime);
        var notJoinedChat = await CreateChatWithChannelsAsync(baseTime.AddMinutes(1));

        joinedChat.Join(user.Id, user.Username, baseTime);
        joinedChat.Join(otherUser.Id, otherUser.Username, baseTime);

        notJoinedChat.Join(otherUser.Id, otherUser.Username, baseTime);

        await Context.SaveChangesAsync();

        var member = Context.ChatMembers.First(x => x.ChatId == joinedChat.Id && x.UserId == user.Id);
        member.ReadUpTo(baseTime.AddMinutes(10));
        await Context.SaveChangesAsync();

        joinedChat.AddMessage(otherUser.Id, "Unread in joined chat", baseTime.AddMinutes(11), _previewService);
        notJoinedChat.AddMessage(otherUser.Id, "Unread in not joined chat", baseTime.AddMinutes(12), _previewService);

        await Context.SaveChangesAsync();

        // Act
        var response = await HttpClient.GetAsync("/api/chats/unread-count");

        // Assert
        response.EnsureSuccessStatusCode();

        var unreadCount = await response.Content.ReadFromJsonAsync<int>();
        unreadCount.Should().Be(1);
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