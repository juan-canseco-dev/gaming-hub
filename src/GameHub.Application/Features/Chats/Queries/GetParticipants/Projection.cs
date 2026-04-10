using GameHub.Contracts.Profile;
using GameHub.Domain.Chats;
using GameHub.Domain.Channels;
using GameHub.Domain.Users;

namespace GameHub.Application.Features.Chats.Queries.GetParticipants;

internal static class GetChatParticipantsProjections
{
    public static IQueryable<UserDto> ProjectToDto(
        this IQueryable<ChatMember> chatMembers,
        IQueryable<UserProfile> userProfiles)
    {
        return
            from member in chatMembers
            join user in userProfiles
                on member.UserId equals user.Id
            select new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Fullname = user.Fullname
            };
    }
}