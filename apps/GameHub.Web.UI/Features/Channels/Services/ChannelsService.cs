using GameHub.Abstractions.Pagination;
using GameHub.Abstractions.Primitives;
using GameHub.Contracts.Channels;
using GameHub.Contracts.Profile;
using GameHub.Web.UI.Features.Channels.Services.Interfaces;
using GameHub.Web.UI.Features.Chats.Models;
using GameHub.Web.UI.Infrastructure.Http;
using System.Net.Http.Json;

namespace GameHub.Web.UI.Features.Channels.Services;

public class ChannelsService : IChannelsService
{
    private HttpClient _httpClient;

    public ChannelsService(
        HttpClient httpClient
    )
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<Result> JoinAsync(Guid chatId, CancellationToken cancellationToken = default)
    {
        var request = new JoinChatRequest(chatId);
        var response = await _httpClient.PostAsJsonAsync(
           requestUri: "/api/channels/join",
           value: request,
           cancellationToken: cancellationToken
        );
        if (response.IsSuccessStatusCode)
        {
            return Result.Success();
        }
        return Result.Failure(await response.ReadProblemAsync());
    }

    public async Task<Result<List<ChannelDto>>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetAsync("channels", cancellationToken);
        if (result.IsSuccessStatusCode)
        {
            var channels = await result.Content.ReadFromJsonAsync<List<ChannelDto>>(cancellationToken: cancellationToken);
            return Result.Success(channels!);
        }
        return Result.Failure<List<ChannelDto>>(await result.ReadProblemAsync());
    }

    public async Task<Result<CursorPage<UserDto>>> GetParticipantsAsync(
        Guid chatId, 
        int limit = 50, 
        string? cursor = null,
        CancellationToken cancellationToken = default
    )
    {
        var uri = cursor is null
           ? $"chat/{chatId}/members?limit={limit}"
           : $"chat/{chatId}/members?limit={limit}&cursor={cursor}";

        var result = await _httpClient.GetAsync(uri, cancellationToken);
        if (result.IsSuccessStatusCode)
        {
            var page = await result.Content.ReadFromJsonAsync<CursorPage<UserDto>>(cancellationToken: cancellationToken);
            return Result.Success(page!);
        }
        return Result.Failure<CursorPage<UserDto>>(await result.ReadProblemAsync());
    }

    public async Task<Result<int>> GetParticipantsCountAsync(Guid chatId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"chat/{chatId}/members/count", cancellationToken);
        if (response.IsSuccessStatusCode) 
        {
            var count = await response.Content.ReadFromJsonAsync<int>(cancellationToken: cancellationToken);
            return Result.Success(count);
        }
        return Result.Failure<int>(await response.ReadProblemAsync());
    }
}
