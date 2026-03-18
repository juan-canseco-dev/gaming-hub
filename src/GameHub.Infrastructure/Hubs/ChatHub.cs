using GameHub.Application.Abstractions.Realtime.Chats;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GameHub.Infrastructure.Hubs;

[Authorize]
public sealed class ChatHub : Hub<IChatClient>
{
    public async Task JoinChat(Guid chatId)
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            GetChatGroupName(chatId)
        );
    }

    public async Task LeaveChat(Guid chatId)
    {
        await Groups.RemoveFromGroupAsync(
           Context.ConnectionId,
           GetChatGroupName(chatId)
        );
    }

    public static string GetChatGroupName(Guid chatId)
       => $"chat:{chatId}";
}
