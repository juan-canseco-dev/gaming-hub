using GameHub.Domain.Abstractions;
using System.Text.Json;

namespace GameHub.Application.Features.Chats.Queries.GetMessages;

public static partial class GetMessagesByChat
{
    internal sealed record Cursor(
        DateTimeOffset CreatedAt,
        Guid MessageId
    )
    {
        public static string Encode(DateTimeOffset createdAt, Guid messageId)
        {
            var cursor = new Cursor(createdAt, messageId);
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
