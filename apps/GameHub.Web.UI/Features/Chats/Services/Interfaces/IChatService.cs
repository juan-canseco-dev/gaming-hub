using GameHub.Abstractions.Pagination;
using GameHub.Abstractions.Primitives;
using GameHub.Contracts.Chats;
using GameHub.Web.UI.Features.Chats.Models;

namespace GameHub.Web.UI.Features.Chats.Services.Interfaces;

public interface IChatService
{
    Task<Result> MarkChatAsReadAsync(Guid chatId, CancellationToken cancellationToken = default);
    Task<Result<MessageDto>> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = default);
    Task<Result<ChatDto>> GetByIdAsync(Guid chatId, CancellationToken cancellationToken = default);
    Task<Result<List<ChatDto>>> GetListAsync(CancellationToken cancellationToken = default);
    Task<Result<CursorPage<MessageDto>>> GetMessagesAsync(
        Guid chatId, 
        int limit = 50,
        string? cursor = null,
        CancellationToken cancellationToken = default
    );
    Task<Result<MessageDto>> GetMessageAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task<Result<int>> GetUnreadMesasgesCount(Guid chatId, CancellationToken cancellationToken = default);

    Task<Result<int>> GetTotalUnreadMesasgesCount(CancellationToken cancellationToken = default);
}
