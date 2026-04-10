using GameHub.Abstractions.Pagination;
using GameHub.Abstractions.Primitives;
using GameHub.Contracts.Chats;
using GameHub.Web.UI.Features.Chats.Models;

namespace GameHub.Web.UI.Features.Chats.Services.Interfaces;

public interface IChatService
{
    Task<Result> MarkChatAsReadAsync(Guid chatId);
    Task<Result<MessageDto>> SendMessageAsync(SendMessageRequest request);
    Task<Result<ChatDto>> GetByIdAsync(Guid chatId);
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
