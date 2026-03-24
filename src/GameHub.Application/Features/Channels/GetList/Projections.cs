using GameHub.Contracts.Channels;
using GameHub.Domain.Chats;

namespace GameHub.Application.Features.Channels.GetList;

internal static class ChannelProjections
{
    public static IQueryable<Chat> ApplySortingByChannel (this IQueryable<Chat> query)
    {
        return query.OrderBy(c => c.ChannelId);
    }

    public static IQueryable<ChannelDto> ProjectToResponse(this IQueryable<Chat> query)
    {
        return query.Select(chat => new ChannelDto(
            chat.Channel.Id,
            chat.Id,
            chat.Channel.Slug,
            chat.Channel.Description,
            chat.Members.Count
        ));
    }
}