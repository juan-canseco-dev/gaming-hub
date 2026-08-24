using GameHub.Application.Abstractions.Clock;
using GameHub.Domain.Chats;
using GameHub.Domain.Channels;
using GameHub.Domain.Users;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Infrastructure.Data.Seed.Development;

internal sealed class DemoChatSeeder : IDevelopmentDataSeeder
{
    private const int JoinIntervalMinutes = 5;
    private const int MessageIntervalMinutes = 5;

    private readonly ApplicationDbContext _context;
    private readonly IDateTimeProvider _timeProvider;
    private readonly ILogger<DemoChatSeeder> _logger;
    private readonly MessagePreviewService _messagePreviewService;

    public DemoChatSeeder(
        ApplicationDbContext context,
        IDateTimeProvider timeProvider,
        ILogger<DemoChatSeeder> logger,
        MessagePreviewService messagePreviewService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _messagePreviewService = messagePreviewService ?? throw new ArgumentNullException(nameof(messagePreviewService));
    }

    public int Order => 2;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {

        _logger.LogInformation("Starting demo chat seeding");

        var users = await _context.UserProfiles
            .ToListAsync(cancellationToken);

        var chats = await _context.Chats
            .ToListAsync(cancellationToken);

        if (await _context.ChatMembers.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Skipping demo chat seeding because memberships already exist");
            return;
        }

        var joinedCount = SeedParticipants(users, chats);
        var messageCount = SeedPresentationMessages(users, chats);

        _logger.LogInformation(
            "Demo chat seeding completed. Added {JoinedCount} memberships and {MessageCount} presentation messages.",
            joinedCount,
            messageCount);
    }

    private int SeedParticipants(
        IReadOnlyCollection<UserProfile> users,
        IReadOnlyCollection<Chat> chats)
    {
        var joinedCount = 0;
        var joinedAt = _timeProvider.CurrentTimeUtc.AddDays(-14);

        foreach (var chat in chats)
        {
            foreach (var user in users)
            {
                chat.Join(user.Id, user.Username, joinedAt);
                var newUserChat = new UserChat(chat.Id, user.Id, joinedAt);
                _context.UserChats.Add(newUserChat);
                joinedAt = joinedAt.AddMinutes(JoinIntervalMinutes);
                joinedCount++;
            }
        }

        return joinedCount;
    }

    private int SeedPresentationMessages(
        IReadOnlyCollection<UserProfile> users,
        IReadOnlyCollection<Chat> chats)
    {
        var messageCount = 0;
        var messageAt = _timeProvider.CurrentTimeUtc.AddDays(-13);

        foreach (var chat in chats)
        {
            var channelName = Channel.FromValue(chat.ChannelId)?.Name ?? "this channel";

            foreach (var user in users)
            {
                var content = BuildPresentationMessage(user.Fullname, channelName);
                chat.AddMessage(user.Id, content, messageAt, _messagePreviewService);
                messageAt = messageAt.AddMinutes(MessageIntervalMinutes);
                messageCount++;
            }
        }

        return messageCount;
    }

    private static string BuildPresentationMessage(string fullname, string channelName)
        => $"Hi everyone! I'm {fullname}, happy to be part of {channelName}.";
}
