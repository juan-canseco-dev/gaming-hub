

using GameHub.Application.Contracts.Chats;

namespace GameHub.Application.Abstractions.Realtime.Chats;

public interface IMessageSentNotifier
{
    Task NotifyAsync(
        Guid chatId,
        Notification notification,
        CancellationToken cancellationToken = default);

    public sealed record Notification(
        MessageDto Message
    );
}