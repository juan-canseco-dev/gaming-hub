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
using MassTransit.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GameHub.Abstractions.Pagination;

namespace GameHub.WebAPI.IntegrationTests.Features.Chats;

[Collection(SharedTestCollection.FixtureName)]
public class GetMessagesTests(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    private IServiceScope _scope = null!;
    private IApplicationDbContext _context = null!;
    private UserManager<ApplicationUser> _userManager = null!;
    private IIdentityService _identityService = null!;
    private MessagePreviewService _messagePreviewService = null!;

    protected HttpClient HttpClient => factory.HttpClient;
    protected IApplicationDbContext Context => _context;

    public Task InitializeAsync()
    {
        _scope = factory.Services.CreateScope();
        var services = _scope.ServiceProvider;

        _context = services.GetRequiredService<IApplicationDbContext>();
        _userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        _identityService = services.GetRequiredService<IIdentityService>();
        _messagePreviewService = services.GetRequiredService<MessagePreviewService>();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await factory.ResetDatabaseAsync();
        _scope.Dispose();
    }

    [Fact]
    public async Task GetMessages_ShouldReturnAllMessagesAcrossCursorPages()
    {
        const int limit = 6;

        var chat = await CreateChatWithChannelsAsync();
        await CreateAdminUserAsync();

        var userOne = await CreateUserProfileAsync(
            "john.doe@mail.com",
            "john_doe",
            "John Doe");

        var userTwo = await CreateUserProfileAsync(
            "jane.doe@mail.com",
            "jane_doe",
            "Jane Doe");

        chat.Join(userOne.Id, userOne.Username, DateTimeOffset.UtcNow);
        chat.Join(userTwo.Id, userTwo.Username, DateTimeOffset.UtcNow);

        await Context.SaveChangesAsync();

        await SeedAlternatingMessagesAsync(chat, userOne, userTwo, count: 22);

        AuthenticateClient();

        var allMessages = await GetAllPagesAsync(chat.Id, limit);

        allMessages.Count.Should().Be(24);
        MessagesShouldMatch(chat.Messages.ToList(), allMessages);
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

    private async Task SeedAlternatingMessagesAsync(
        Chat chat,
        UserProfile userOne,
        UserProfile userTwo,
        int count)
    {
        var createdAt = DateTimeOffset.UtcNow;

        for (int i = 1; i <= count; i++)
        {
            var sender = i % 2 == 1 ? userOne : userTwo;
            var senderLabel = i % 2 == 1 ? "userOne" : "userTwo";

            chat.AddMessage(
                sender.Id,
                $"Message {i} from {senderLabel}",
                createdAt,
                _messagePreviewService);

            createdAt = createdAt.AddMinutes(5);
        }

        await Context.SaveChangesAsync();
    }

    private void AuthenticateClient()
    {
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                TestJwtTokenGenerator.Generate(Guid.NewGuid(), "random_mail@mail.com"));
    }

    private async Task<List<MessageDto>> GetAllPagesAsync(Guid chatId, int limit)
    {
        var allMessages = new List<MessageDto>();
        string? cursor = null;

        do
        {
            var page = await GetMessagesPageAsync(chatId, limit, cursor);

            allMessages.AddRange(page.Items);
            cursor = page.Next;
        }
        while (cursor is not null);

        return allMessages
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToList();
    }

    private async Task<CursorPage<MessageDto>> GetMessagesPageAsync(Guid chatId, int limit, string? cursor = null)
    {
        var uri = cursor is null
            ? $"/api/chat/{chatId}/messages?limit={limit}"
            : $"/api/chat/{chatId}/messages?limit={limit}&cursor={cursor}";

        var response = await HttpClient.GetAsync(uri);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CursorPage<MessageDto>>();
        result.Should().NotBeNull();

        return result!;
    }

    private static void MessagesShouldMatch(
        IReadOnlyList<ChatMessage> expectedMessages,
        IReadOnlyList<MessageDto> actualMessages)
    {
        actualMessages.Should().HaveCount(expectedMessages.Count);

        for (var i = 0; i < expectedMessages.Count; i++)
        {
            var expected = expectedMessages[i];
            var actual = actualMessages[i];

            actual.Id.Should().Be(expected.Id);
            actual.CreatedAt.Should().Be(expected.CreatedAt);
            actual.Content.Should().Be(expected.Content);
            actual.User.Should().NotBeNull();
            actual.User.Id.Should().Be(expected.SenderUserId);
        }
    }
}