using GameHub.Abstractions.Pagination;
using GameHub.Abstractions.Primitives;
using GameHub.Contracts.Chats;
using GameHub.Web.UI.Services.Interfaces;
using System.Net;
using System.Net.Http.Json;
using GameHub.Web.UI.Models;

namespace GameHub.Web.UI.Services;

public class ChatService : IChatService
{
    private readonly HttpClient _httpClient;

    public ChatService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<Result> MarkChatAsReadAsync(Guid chatId)
    {
        var response = await _httpClient.PostAsync($"chats/{chatId}/read", content: null);
        if (response.IsSuccessStatusCode)
        {
            return Result.Success();
        }
        if (response.StatusCode == HttpStatusCode.NotFound ||
          response.StatusCode == HttpStatusCode.BadRequest
        )
        {
            var error = await response.Content.ReadFromJsonAsync<Error>();
            return Result.Failure(error!);
        }
        return Result.Failure(Error.InternalServerError);
    }

    public async Task<Result> SendMessageAsync(SendMessageRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync(
            requestUri: $"chats/messages",
            value: request
        );

        if (response.IsSuccessStatusCode)
        {
            return Result.Success();
        }
        if (response.StatusCode == HttpStatusCode.NotFound ||
          response.StatusCode == HttpStatusCode.BadRequest
        )
        {
            var error = await response.Content.ReadFromJsonAsync<Error>();
            return Result.Failure(error!);
        }
        return Result.Failure(Error.InternalServerError);
    }

    public async Task<Result<List<ChatDto>>> GetListAsync()
    {
        var response = await _httpClient.GetAsync("chats");
        if(response.IsSuccessStatusCode)
        {
            var chats = await response.Content.ReadFromJsonAsync<List<ChatDto>>();
            return Result.Success(chats!);
        }
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadFromJsonAsync<Error>();
            return Result.Failure<List<ChatDto>>(error!);
        }
        return Result.Failure<List<ChatDto>>(Error.InternalServerError);
    }

    public async Task<Result<CursorPage<MessageDto>>> GetMessagesAsync(Guid chatId, int limit = 50, string? cursor = null)
    {
        var uri = cursor is null
            ? $"chat/{chatId}/messages?limit={limit}"
            : $"chat/{chatId}/messages?limit={limit}&cursor={cursor}";

        var response = await _httpClient.GetAsync(uri);
        
        if (response.IsSuccessStatusCode)
        {
            var page = await response.Content.ReadFromJsonAsync<CursorPage<MessageDto>>();
            return Result.Success(page!);
        }
        if (response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.BadRequest
        )
        {
            var error = await response.Content.ReadFromJsonAsync<Error>();
            return Result.Failure<CursorPage<MessageDto>>(error!);
        }
        return Result.Failure<CursorPage<MessageDto>>(Error.InternalServerError);
    }

    public async Task<Result<MessageDto>> GetMessageAsync(Guid messageId)
    {
        var response = await _httpClient.GetAsync($"chat/messages/{messageId}");
        

        if (response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadFromJsonAsync<MessageDto>();
            return Result.Success(message!); 
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var error = await response.Content.ReadFromJsonAsync<Error>();
            return Result.Failure<MessageDto>(error!);
        }

        return Result.Failure<MessageDto>(Error.InternalServerError);
    }
    public async Task<Result<int>> GetUnreadMesasgesCount(Guid chatId)
    {

        var response = await _httpClient.GetAsync($"chats/{chatId}/messages/unread/count");
        if (response.IsSuccessStatusCode)
        {
            var count = await response.Content.ReadFromJsonAsync<int>();
            return Result.Success(count);
        }
        if (response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.BadRequest
        )
        {
            var error = await response.Content.ReadFromJsonAsync<Error>();
            return Result.Failure<int>(error!);
        }
        return Result.Failure<int>(Error.InternalServerError);
    }

    public async Task<Result<int>> GetTotalUnreadMesasgesCount()
    {
        var response = await _httpClient.GetAsync("chats/unread-count");
        if (response.IsSuccessStatusCode)
        {
            var unreadCount = await response.Content.ReadFromJsonAsync<int>();
            return Result.Success(unreadCount);
        }
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadFromJsonAsync<Error>();
            return Result.Failure<int>(error!);
        }
        return Result.Failure<int>(Error.InternalServerError);
    }
}
