using GameHub.Contracts.Notifications;
namespace GameHub.Application.Abstractions.Realtime.Chats;

public interface IChatClient
{
    Task MessageSent(MessageNotification notification);
    Task UserJoinedChat(UserJoinedNotification notification);
}
