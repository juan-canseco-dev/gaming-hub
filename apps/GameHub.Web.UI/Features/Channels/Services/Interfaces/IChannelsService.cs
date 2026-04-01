using GameHub.Abstractions.Pagination;
using GameHub.Abstractions.Primitives;
using GameHub.Contracts.Channels;
using GameHub.Contracts.Profile;

namespace GameHub.Web.UI.Features.Channels.Services.Interfaces;

public interface IChannelsService
{
    Task<Result> JoinAsync(Guid chatId);
    Task<Result<List<ChannelDto>>> GetListAsync();
    Task<Result<CursorPage<UserDto>>> GetParticipantsAsync(
        Guid chatId,
        int limit = 50,
        string? cursor = null
    );
    Task<Result<int>> GetParticipantsCountAsync(Guid chatId);
}
