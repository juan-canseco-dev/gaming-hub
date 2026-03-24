using GameHub.Abstractions.Primitives;
using System.Text.Json;

namespace GameHub.Application.Features.Chats.Queries.GetParticipants;

public static partial class GetChatParticipants
{
    internal sealed record Cursor(
        string Username,
        Guid UserId
    )
    {
        public static string Encode(string username, Guid userId)
        {
            var cursor = new Cursor(username, userId);
            var json = JsonSerializer.Serialize(cursor);
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
        }

        public static Result<Cursor> Decode(string encodedCursor)
        {
            try
            {
                var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encodedCursor));
                var cursor = JsonSerializer.Deserialize<Cursor>(json);
                if (cursor is null)
                {
                    return Result.Failure<Cursor>(Errors.InvalidCursor);
                }
                return Result.Success(cursor);
            }
            catch (Exception)
            {
                return Result.Failure<Cursor>(Errors.InvalidCursor);
            }
        }
    }
}
