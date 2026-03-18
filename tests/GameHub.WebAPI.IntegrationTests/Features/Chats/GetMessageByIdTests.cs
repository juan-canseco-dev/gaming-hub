using FluentAssertions;
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Identity;
using GameHub.Application.Contracts.Identity;
using GameHub.Domain.Chats;
using GameHub.Domain.Users;
using GameHub.Infrastructure.Identity.Models;
using GameHub.WebAPI.IntegrationTests.Abstractions;
using GameHub.WebAPI.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Net;
using System.Net.Http.Json;
using GameHub.Application.Contracts.Chats;

namespace GameHub.WebAPI.IntegrationTests.Features.Chats;


[Collection(SharedTestCollection.FixtureName)]
public class GetMessageByIdTests(CustomWebApplicationFactory factory) : IAsyncLifetime
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

    public async Task GetMessageById_ShouldReturnNotFound_WhenMessage_DoNotExists()
    {
        AuthenticateClient();
        var response = await HttpClient.GetAsync($"/api/chat/messages/{Guid.Empty}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMessageById_ShouldReturnOk_When_Id_IsValid()
    {
        var chat = await CreateChatWithChannelsAsync();
        await CreateAdminUserAsync();

        var user = await CreateUserProfileAsync(
            "john.doe@mail.com",
            "john_doe",
            "John Doe");

        var joinedMessage = chat.Join(user.Id, user.Username, DateTimeOffset.UtcNow);

        await Context.SaveChangesAsync();

        var response = await HttpClient.GetAsync($"/api/chat/messages/{joinedMessage.Id}");
        response.EnsureSuccessStatusCode();

        var messageDto = await response.Content.ReadFromJsonAsync<MessageDto>();
        messageDto.Should().NotBeNull();    

        messageDto.Id.Should().Be(joinedMessage.Id);
        messageDto.Content.Should().Be(joinedMessage.Content);
        messageDto.CreatedAt.Should().Be(joinedMessage.CreatedAt); 
        messageDto.User.Should().NotBeNull();
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

    private void AuthenticateClient()
    {
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                TestJwtTokenGenerator.Generate(Guid.NewGuid(), "random_mail@mail.com"));
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
