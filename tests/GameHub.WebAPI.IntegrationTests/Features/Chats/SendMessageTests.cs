using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Identity;
using GameHub.WebAPI.IntegrationTests.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using GameHub.WebAPI.IntegrationTests.Helpers;
using System.Net.Http.Headers;
using GameHub.Application.Features.Chats.Commands.SendMessage;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MassTransit.Testing;
using GameHub.Contracts.Identity;
using GameHub.Domain.Chats;
using Microsoft.AspNetCore.Identity;
using GameHub.Infrastructure.Identity.Models;
using GameHub.Domain.Users;
using GameHub.EventBus.Contracts;

namespace GameHub.WebAPI.IntegrationTests.Features.Chats;

[Collection(SharedTestCollection.FixtureName)]
public class SendMessageTests(CustomWebApplicationFactory factory) : IAsyncLifetime
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

        Context.UserProfiles.Add( userProfile );
        
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
    public async Task SendMesasge_ShouldReturn_NotFound_When_ChatNotExists()
    {
        factory.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenGenerator.Generate(Guid.NewGuid(), "randmom_mail@mail.com"));

        var notExistentChatId = Guid.NewGuid();

        var request = new ChatSendMessage.Command(notExistentChatId, "!Hello World!");
        
        var response = await factory.HttpClient.PostAsJsonAsync(
            requestUri: "/api/chats/messages",
            value: request
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SendMessage_ShouldReturn_Ok_When_Command_IsValid()
    {
        var chat = await SetupChatAndChannels();
        chat.Id.Should().NotBeEmpty();
        await SetupAdminUser();
        var userProfile = await SetupUser();
        userProfile.Should().NotBeNull();

        chat.Join(userProfile.Id, userProfile.Username, userProfile.CreatedAt);

        await Context.SaveChangesAsync();

        factory.HttpClient.DefaultRequestHeaders.Authorization =
         new AuthenticationHeaderValue("Bearer", TestJwtTokenGenerator.Generate(userProfile.Id, userProfile.Email));

        await TestHarness.Start();

        var request = new ChatSendMessage.Command(chat.Id, "Hello Every One :)");

        var response = await factory.HttpClient.PostAsJsonAsync(
          requestUri: "/api/chats/messages",
          value: request
        );

        var result = response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var eventPublished = await TestHarness.Published.Any<ChatMessageSentEvent>();
        eventPublished.Should().BeTrue();   

        await TestHarness.Stop();
    }

    [Fact]
    public async Task SendMessaged_ShouldReturn_BadRequest_WhenUserIsNotParticipant()
    {
        var chat = await SetupChatAndChannels();
        chat.Id.Should().NotBeEmpty();

        await SetupAdminUser();
        var userProfile = await SetupUser();
        userProfile.Should().NotBeNull();

        await Context.SaveChangesAsync();

        factory.HttpClient.DefaultRequestHeaders.Authorization =
         new AuthenticationHeaderValue("Bearer", TestJwtTokenGenerator.Generate(userProfile.Id, userProfile.Email));

        await TestHarness.Start();

        var request = new ChatSendMessage.Command(chat.Id, "Hello Every One :)");

        var response = await factory.HttpClient.PostAsJsonAsync(
          requestUri: "/api/chats/messages",
          value: request
        );

        var result = response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    }

}
