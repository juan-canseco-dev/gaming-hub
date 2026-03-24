using GameHub.Abstractions.Primitives;

namespace GameHub.Domain.Users;

public class UserProfileErrors
{
    public static Error NotFound(Guid userId)
    {
        return new Error(
            Code: "User.NotFound",
            Description: $"The specified User with the Id: {userId} Was Not Found."
        );
    }
}
