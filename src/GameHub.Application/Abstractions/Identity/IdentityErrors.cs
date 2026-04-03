using GameHub.Abstractions.Primitives;

namespace GameHub.Application.Abstractions.Identity;

public static class IdentityErrors
{
    public static Error InvalidCredentials => new(
      Code: "User.InvalidCredentials",
      Description: "Invalid username and/or password."
    );
    public static Error EmailAlreadyExists => new(
       Code: "User.EmailAlreadyExists",
       Description: "The specified email address is already in use by another user."
    );
    public static Error UsernameAlreadyExists => new(
      Code: "User.UsernameAlreadyExists",
      Description: "The specified username address is already in use by another user."
   );
}
