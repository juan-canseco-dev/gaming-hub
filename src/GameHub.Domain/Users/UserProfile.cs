using GameHub.Domain.Abstractions;

namespace GameHub.Domain.Users;

public class UserProfile : Entity<Guid>
{
    public string Email { get; private init; } = default!;
    public string Username { get; private init; } = default!;
    public string Fullname { get; private init; } = default!;
    public DateTimeOffset CreatedAt { get; private init; } = default!;
    private UserProfile() { }
    public UserProfile(
        Guid id, 
        string email,
        string username, 
        string fullname,
        DateTimeOffset createdAt
    ) : base(id)
    {
        Email = email;
        Username = username;
        Fullname = fullname;
        CreatedAt = createdAt;
    }
}
