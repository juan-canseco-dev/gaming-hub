using GameHub.Application.Contracts.Chats;

namespace GameHub.Application.Abstractions.Realtime.Chats;

public interface IUserJoinedChatNotifier
{
    Task NotifyAsync(
        Guid chatId,
        Notification notification,
        CancellationToken cancellationToken = default);


    public sealed record Notification(
        int NumberOfParticipants,
        MessageDto Message
    );
}