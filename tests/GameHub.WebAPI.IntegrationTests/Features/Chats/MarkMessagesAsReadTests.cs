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
public class MarkMessagesAsReadTests(CustomWebApplicationFactory factory) : IAsyncLifetime
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
    public async Task MarkMessagesAsRead_ShouldReturnNotFound_WhenChatDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AuthenticateClient(userId);

        // Act
        var response = await HttpClient.PostAsync($"/api/chats/{Guid.NewGuid()}/read", content: null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MarkMessagesAsRead_ShouldReturnBadRequest_WhenUserIsNotParticipant()
    {
        // Arrange
        var user = await CreateUserProfileAsync(
            "john.doe@mail.com",
            "john_doe",
            "John Doe");

        AuthenticateClient(user.Id);

        var chat = await CreateChatWithChannelsAsync();

        // Act
        var response = await HttpClient.PostAsync($"/api/chats/{chat.Id}/read", content: null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await response.Content.ReadFromJsonAsync<Error>();
        error.Should().NotBeNull();
        error!.Code.Should().Be(ChatErrors.NotParticipant(user.Id).Code);
    }

    [Fact]
    public async Task MarkMessagesAsRead_ShouldReturnOk_AndUpdateLastReadAt_WhenUserIsParticipant()
    {
        // Arrange
        await CreateAdminUserAsync();

        var user = await CreateUserProfileAsync(
            "john.doe@mail.com",
            "john_doe",
            "John Doe");

        AuthenticateClient(user.Id);

        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        var chat = await CreateChatWithChannelsAsync(createdAt);

        chat.Join(user.Id, user.Username, createdAt);
        await Context.SaveChangesAsync();

        var membershipBefore = Context.ChatMembers
            .First(x => x.ChatId == chat.Id && x.UserId == user.Id);

        membershipBefore.LastReadAt.Should().BeNull();

        // Act
        var response = await HttpClient.PostAsync($"/api/chats/{chat.Id}/read", content: null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var assertScope = factory.Services.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var membershipAfter = assertContext.ChatMembers
            .First(x => x.ChatId == chat.Id && x.UserId == user.Id);

        membershipAfter.LastReadAt.Should().BeAfter(createdAt);
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
