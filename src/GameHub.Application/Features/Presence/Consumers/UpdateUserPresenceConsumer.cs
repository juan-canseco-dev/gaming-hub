using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Clock;
using GameHub.Application.Abstractions.Realtime.Presence;
using GameHub.Contracts.Presence;
using GameHub.Domain.Presence;
using GameHub.Contracts.Notifications;
using GameHub.EventBus.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameHub.Application.Features.Presence.Consumers;

public class UpdateUserPresenceConsumer : IConsumer<UserPresenceUpdateEvent>
{
    private readonly IUpdatePresenceNotifier _notifier;
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _timeProvider;
    private readonly ILogger<UpdateUserPresenceConsumer> _logger;

    public UpdateUserPresenceConsumer(
        IUpdatePresenceNotifier notifier, 
        IApplicationDbContext context, 
        IDateTimeProvider timeProvider,
        ILogger<UpdateUserPresenceConsumer> logger)
    {
        _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Consume(ConsumeContext<UserPresenceUpdateEvent> context)
    {
        var message = context.Message;

        _logger.LogDebug(
            "Consuming {EventName} for UserId {UserId}.",
            nameof(UserPresenceUpdateEvent),
            message.UserId
         );

        var userChats = await _context.UserChats
            .Where(x => x.UserId == message.UserId)
            .Select(x => x.ChatId)
            .Distinct()
            .ToListAsync(context.CancellationToken);

        if (userChats.Count == 0)
        {
            _logger.LogDebug(
                "Presence update for UserId {UserId} has no chats to notify",
                message.UserId);
            return;
        }

        var currentTime = _timeProvider.CurrentTimeUtc;
        var userPresence = new UserPresence(message.UserId, message.LastActive);
        var presence = new UserPresenceDto(
            message.UserId,
            message.LastActive,
            userPresence.GetStatus(currentTime).Name);
        var onlineCutoff = UserPresence.GetOnlineCutoff(currentTime);
        var onlineCounts = await (
                from member in _context.ChatMembers
                join memberPresence in _context.UserPresences on member.UserId equals memberPresence.UserId
                where userChats.Contains(member.ChatId) && memberPresence.LastActive >= onlineCutoff
                group member by member.ChatId into membersByChat
                select new { ChatId = membersByChat.Key, Count = membersByChat.Count() })
            .ToDictionaryAsync(x => x.ChatId, x => x.Count, context.CancellationToken);

        foreach (var chatId in userChats)
        {
            var notification = new UserPresenceUpdatedNotification(
                presence,
                onlineCounts.GetValueOrDefault(chatId)
            );

            await _notifier.NotifyAsync(
                chatId, 
                notification, 
                context.CancellationToken
            );

        }

        _logger.LogDebug(
            "Processed {EventName} for UserId {UserId}; notified {ChatCount} chats",
            nameof(UserPresenceUpdateEvent),
            message.UserId,
            userChats.Count);
    }
}
