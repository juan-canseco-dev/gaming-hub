using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using GameHub.Application.Features.Presence.Commands.Update;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GameHub.Infrastructure.Hubs;

[Authorize]
public sealed class ChatHub : Hub<ChatClientAdapter>
{
    private readonly ISender _sender;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ISender sender, ILogger<ChatHub> logger)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task JoinChat(Guid chatId)
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            GetChatGroupName(chatId)
        );

        _logger.LogDebug(
            "SignalR connection {ConnectionId} joined ChatId {ChatId}",
            Context.ConnectionId,
            chatId);
    }

    public async Task LeaveChat(Guid chatId)
    {
        await Groups.RemoveFromGroupAsync(
           Context.ConnectionId,
           GetChatGroupName(chatId)
        );

        _logger.LogDebug(
            "SignalR connection {ConnectionId} left ChatId {ChatId}",
            Context.ConnectionId,
            chatId);
    }

    public async Task UpdatePresence()
    {
        var result = await _sender.Send(new UpdatePresence.Command(), Context.ConnectionAborted);
        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Presence update failed for SignalR connection {ConnectionId} with {ErrorCode}",
                Context.ConnectionId,
                result.Error.Code);
            throw new HubException(result.Error.Description);
        }
    }

    public static string GetChatGroupName(Guid chatId)
       => $"chat:{chatId}";
}
