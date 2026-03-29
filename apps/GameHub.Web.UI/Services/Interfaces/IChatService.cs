using GameHub.Abstractions.Pagination;
using GameHub.Abstractions.Primitives;
using GameHub.Contracts.Chats;
using GameHub.Web.UI.Models;

namespace GameHub.Web.UI.Services.Interfaces;

public interface IChatService
{
    Task<Result> MarkChatAsReadAsync(Guid chatId);
    Task<Result> SendMessageAsync(SendMessageRequest request);
    Task<Result<List<ChatDto>>> GetListAsync();
    Task<Result<CursorPage<MessageDto>>> GetMessagesAsync(
        Guid chatId, 
        int limit = 50,
        string? cursor = null
    );
    Task<Result<MessageDto>> GetMessageAsync(Guid messageId);
    Task<Result<int>> GetUnreadMesasgesCount(Guid chatId);

    Task<Result<int>> GetTotalUnreadMesasgesCount();
}
