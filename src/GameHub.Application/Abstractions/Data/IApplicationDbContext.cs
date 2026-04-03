using GameHub.Domain.Chats;
using GameHub.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Application.Abstractions.Data;

public interface IApplicationDbContext
{
    public DbSet<Channel> Channels { get; }
    public DbSet<Chat> Chats { get; }
    public DbSet<ChatMessage> ChatMessages { get; }
    public DbSet<ChatMember> ChatMembers { get; }
    public DbSet<UserProfile> UserProfiles { get; }
    public DbSet<UserChat> UserChats { get;  }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
