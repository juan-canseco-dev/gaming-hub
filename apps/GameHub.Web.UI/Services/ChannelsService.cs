using GameHub.Abstractions.Pagination;
using GameHub.Abstractions.Primitives;
using GameHub.Contracts.Channels;
using GameHub.Contracts.Profile;
using GameHub.Web.UI.Models;
using GameHub.Web.UI.Services.Interfaces;
using System.Net;
using System.Net.Http.Json;

namespace GameHub.Web.UI.Services;

public class ChannelsService : IChannelsService
{
    private HttpClient _httpClient;

    public ChannelsService(
        HttpClient httpClient
    )
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<Result> JoinAsync(Guid chatId)
    {
        var request = new JoinChatRequest(chatId);
        var response = await _httpClient.PostAsJsonAsync(
           requestUri: "/api/channels/join",
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

    public async Task<Result<List<ChannelDto>>> GetListAsync()
    {
        var result = await _httpClient.GetAsync("channels");
        if (result.IsSuccessStatusCode)
        {
            var channels = await result.Content.ReadFromJsonAsync<List<ChannelDto>>();
            return Result.Success(channels!);
        }
        if (result.StatusCode == HttpStatusCode.BadRequest)
        {
            var error = await result.Content.ReadFromJsonAsync<Error>();
            return Result.Failure<List<ChannelDto>>(error!);
        }
        return Result.Failure<List<ChannelDto>>(Error.InternalServerError);
    }

    public async Task<Result<CursorPage<UserDto>>> GetParticipantsAsync(
        Guid chatId, 
        int limit = 50, 
        string? cursor = null
    )
    {
        var uri = cursor is null
           ? $"chat/{chatId}/members?limit={limit}"
           : $"chat/{chatId}/members?limit={limit}&cursor={cursor}";

        var result = await _httpClient.GetAsync(uri);
        if (result.IsSuccessStatusCode)
        {
            var page = await result.Content.ReadFromJsonAsync<CursorPage<UserDto>>();
            return Result.Success(page!);
        }
        if (result.StatusCode == HttpStatusCode.BadRequest)
        {
            var error = await result.Content.ReadFromJsonAsync<Error>();
            return Result.Failure<CursorPage<UserDto>>(error!);
        }
        return Result.Failure<CursorPage<UserDto>>(Error.InternalServerError);
    }

    public async Task<Result<int>> GetParticipantsCountAsync(Guid chatId)
    {
        var response = await _httpClient.GetAsync($"chat/{chatId}/members/count");
        if (response.IsSuccessStatusCode) 
        {
            var count = await response.Content.ReadFromJsonAsync<int>();
            return Result.Success(count);
        }
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadFromJsonAsync<Error>();
            return Result.Failure<int>(error!);
        }
        return Result.Failure<int>(Error.InternalServerError);
    }
}
