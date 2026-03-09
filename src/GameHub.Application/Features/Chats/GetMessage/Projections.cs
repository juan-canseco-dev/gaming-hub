using GameHub.Application.Contracts.Chats;
using GameHub.Application.Contracts.Profile;
using GameHub.Domain.Chats;
using GameHub.Domain.Users;

namespace GameHub.Application.Features.Chats.GetMessage;

internal static class GetMessageByIdProjections
{
    public static IQueryable<MessageDto> ProjectToDto(
      this IQueryable<ChatMessage> messages,
      IQueryable<UserProfile> userProfiles)
    {
        return
            from message in messages
            join user in userProfiles
                on message.SenderUserId equals user.Id into users
            from user in users.DefaultIfEmpty()
            select new MessageDto
            {
                Id = message.Id,
                Content = message.Content,
                CreatedAt = message.CreatedAt.UtcDateTime,
                IsSystem = message.Type == ChatMessageType.System,
                User = user == null
                    ? null
                    : new UserDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Fullname = user.Fullname
                    }
            };
    }
}
