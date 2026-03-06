namespace GameHub.Domain.Users;

public static class SystemUsers
{
    public static readonly Guid AdminUserId =
        Guid.Parse("019cc46e-98c2-721a-aac6-6cacdf4f52bf");

    public static readonly string AdminUsername = "system-admin";

    public static readonly string AdminEmail = "system@gaminghub.local";
}