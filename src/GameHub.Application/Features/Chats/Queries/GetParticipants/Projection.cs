using GameHub.Domain.Chats;
using GameHub.Domain.Users;
using GameHub.Domain.Presence;

namespace GameHub.Application.Features.Chats.Queries.GetParticipants;

internal static class GetChatParticipantsProjections
{
    public static IQueryable<ParticipantProjection> ProjectToParticipant(
        this IQueryable<ChatMember> chatMembers,
        IQueryable<UserProfile> userProfiles,
        IQueryable<UserPresence> userPresences)
    {
        return
            from member in chatMembers
            join user in userProfiles
                on member.UserId equals user.Id
            join presence in userPresences
                on user.Id equals presence.UserId
            select new ParticipantProjection
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Fullname = user.Fullname,
                Presence = presence
            };
    }
}

internal sealed class ParticipantProjection
{
    public Guid Id { get; init; }
    public string Username { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string Fullname { get; init; } = null!;
    public UserPresence Presence { get; init; } = null!;
}
