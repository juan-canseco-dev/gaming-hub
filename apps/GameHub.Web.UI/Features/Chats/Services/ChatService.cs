using GameHub.Abstractions.Pagination;
using GameHub.Abstractions.Primitives;
using GameHub.Contracts.Chats;
using GameHub.Web.UI.Infrastructure.Http;
using System.Net.Http.Json;
using GameHub.Web.UI.Features.Chats.Models;
using GameHub.Web.UI.Features.Chats.Services.Interfaces;

namespace GameHub.Web.UI.Features.Chats.Services;

public class ChatService : IChatService
{
    private readonly HttpClient _httpClient;

    public ChatService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<Result> MarkChatAsReadAsync(Guid chatId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"chats/{chatId}/read", content: null, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return Result.Success();
        }
        return Result.Failure(await response.ReadProblemAsync());
    }

    public async Task<Result<MessageDto>> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            requestUri: $"chats/messages",
            value: request,
            cancellationToken: cancellationToken
        );

        if (response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadFromJsonAsync<MessageDto>(cancellationToken: cancellationToken);
            return Result.Success(message!);
        }
        return Result.Failure<MessageDto>(await response.ReadProblemAsync());
    }

    public async Task<Result<ChatDto>> GetByIdAsync(Guid chatId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"chats/{chatId}", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var chat = await response.Content.ReadFromJsonAsync<ChatDto>(cancellationToken: cancellationToken);
            return Result.Success(chat!);
        }
        return Result.Failure<ChatDto>(await response.ReadProblemAsync());
    }

    public async Task<Result<List<ChatDto>>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("chats", cancellationToken);
        if(response.IsSuccessStatusCode)
        {
            var chats = await response.Content.ReadFromJsonAsync<List<ChatDto>>(cancellationToken: cancellationToken);
            return Result.Success(chats!);
        }
        return Result.Failure<List<ChatDto>>(await response.ReadProblemAsync());
    }

    public async Task<Result<CursorPage<MessageDto>>> GetMessagesAsync(Guid chatId, int limit = 50, string? cursor = null, CancellationToken cancellationToken = default)
    {
        var uri = cursor is null
            ? $"chat/{chatId}/messages?limit={limit}"
            : $"chat/{chatId}/messages?limit={limit}&cursor={cursor}";

        var response = await _httpClient.GetAsync(uri, cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            var page = await response.Content.ReadFromJsonAsync<CursorPage<MessageDto>>(cancellationToken: cancellationToken);
            return Result.Success(page!);
        }
        return Result.Failure<CursorPage<MessageDto>>(await response.ReadProblemAsync());
    }

    public async Task<Result<MessageDto>> GetMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"chat/messages/{messageId}", cancellationToken);
        

        if (response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadFromJsonAsync<MessageDto>(cancellationToken: cancellationToken);
            return Result.Success(message!); 
        }

        return Result.Failure<MessageDto>(await response.ReadProblemAsync());
    }
    public async Task<Result<int>> GetUnreadMesasgesCount(Guid chatId, CancellationToken cancellationToken = default)
    {

        var response = await _httpClient.GetAsync($"chats/{chatId}/messages/unread/count", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var count = await response.Content.ReadFromJsonAsync<int>(cancellationToken: cancellationToken);
            return Result.Success(count);
        }
        return Result.Failure<int>(await response.ReadProblemAsync());
    }

    public async Task<Result<int>> GetTotalUnreadMesasgesCount(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("chats/unread-count", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var unreadCount = await response.Content.ReadFromJsonAsync<int>(cancellationToken: cancellationToken);
            return Result.Success(unreadCount);
        }
        return Result.Failure<int>(await response.ReadProblemAsync());
    }
}
