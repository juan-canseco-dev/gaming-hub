namespace GameHub.Application.Abstractions.Realtime.Chats;

public interface IChatClient
{
    Task MessageSent(IMessageSentNotifier.Notification notification);
    Task UserJoinedChat(IUserJoinedChatNotifier.Notification notification);
}
