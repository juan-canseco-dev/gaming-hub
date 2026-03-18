

using FluentAssertions;
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Identity;
using GameHub.Application.Contracts.Identity;
using GameHub.Application.Features.Chats.Commands.Join;
using GameHub.Domain.Chats;
using GameHub.Domain.Users;
using GameHub.EventBus.Contracts;
using GameHub.Infrastructure.Identity.Models;
using GameHub.WebAPI.IntegrationTests.Abstractions;
using GameHub.WebAPI.IntegrationTests.Helpers;
using MassTransit.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace GameHub.WebAPI.IntegrationTests.Features.Chats;

[Collection(SharedTestCollection.FixtureName)]
public class JoinChannelTests(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    protected ITestHarness TestHarness { get; private set; } = null!;
    protected IApplicationDbContext Context { get; private set; } = null!;
    protected UserManager<ApplicationUser> UserManager { get; private set; } = null!;
    protected IIdentityService IdentityService { get; private set; } = null!;
    protected IServiceScope Scope { get; private set; } = null!;

    public Task InitializeAsync()
    {
        Scope = factory.Services.CreateScope();
        TestHarness = Scope.ServiceProvider.GetRequiredService<ITestHarness>();
        Context = Scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        UserManager = Scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        IdentityService = Scope.ServiceProvider.GetRequiredService<IIdentityService>();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await factory.ResetDatabaseAsync();
        Scope.Dispose();
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

        var identityResult = await UserManager.CreateAsync(identityUser, SystemUsers.AdminPassword);
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


    private async Task<Chat> SetupChatAndChannels()
    {
        if (!Context.Channels.Any())
        {
            Context.Channels.AddRange(Channel.GetValues());
        }
        var newChat = Chat.Create(1, DateTimeOffset.UtcNow).Value;
        Context.Chats.Add(newChat);
        await Context.SaveChangesAsync();
        return newChat;
    }

    private async Task<UserProfile?> SetupUser()
    {

        var newUserRequest = new RegisterUserRequest
        {
            Fullname = "John Doe",
            Email = "john_doe@mail.com",
            Username = "john_doe",
            Password = "Password.01",
            ConfirmPassword = "Password.01"
        };

        var result = await IdentityService.RegisterAsync(newUserRequest);

        var userId = result.Value;

        var userProfile = await Context.UserProfiles.FindAsync([userId]);

        return userProfile;

    }

    [Fact]
    public async Task JoinChat_When_Chat_DoNotExsists_ShouldReturn_NotFound()
    {
        factory.HttpClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", TestJwtTokenGenerator.Generate(Guid.NewGuid(), "randmom_mail@mail.com"));

        var notExistentChatId = Guid.NewGuid();

        var request = new JoinChat.Command(notExistentChatId);

        var response = await factory.HttpClient.PostAsJsonAsync(
            requestUri: "/api/channels/join",
            value: request
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task JoinChat_When_Command_IsValid_ShouldReturnOk()
    {
        var chat = await SetupChatAndChannels();
        chat.Id.Should().NotBeEmpty();
        await SetupAdminUser();

        var userProfile = await SetupUser();
        userProfile.Should().NotBeNull();

        factory.HttpClient.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", TestJwtTokenGenerator.Generate(userProfile.Id, userProfile.Email));

        await TestHarness.Start();

        var request = new JoinChat.Command(chat.Id);

        var response = await factory.HttpClient.PostAsJsonAsync(
            requestUri: "/api/channels/join",
            value: request
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var eventPublished = await TestHarness.Published.Any<ChatMemberJoinedEvent>();
        eventPublished.Should().BeTrue();

        await TestHarness.Stop();
    }

    [Fact]
    public async Task JoinChat_When_UserAlreadyParticipant_ShouldReturnBadRequest()
    {
        var chat = await SetupChatAndChannels();
        chat.Id.Should().NotBeEmpty();
        await SetupAdminUser();

        var userProfile = await SetupUser();
        userProfile.Should().NotBeNull();

        chat.Join(userProfile.Id, userProfile.Username, DateTimeOffset.UtcNow);

        await Context.SaveChangesAsync();

        factory.HttpClient.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", TestJwtTokenGenerator.Generate(userProfile.Id, userProfile.Email));

        var request = new JoinChat.Command(chat.Id);

        var response = await factory.HttpClient.PostAsJsonAsync(
            requestUri: "/api/channels/join",
            value: request
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    }
}

