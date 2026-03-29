using GameHub.Domain.Chats;
using GameHub.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameHub.Infrastructure.Data.Configurations;

public class ChatMemberConfiguration : IEntityTypeConfiguration<ChatMember>
{
    public void Configure(EntityTypeBuilder<ChatMember> builder)
    {
        builder.ToTable("ChatMembers", "GameHub");

        builder.Property(x => x.Id)
              .HasValueGenerator<UUIDv7Generator>()
              .ValueGeneratedOnAdd();

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ChatId);

        builder.HasOne(x=> x.Chat)
            .WithMany(c => c.Members)
            .HasForeignKey(x => x.ChatId)
            .OnDelete(DeleteBehavior.Cascade);


        builder.Property(i => i.UserId);

        builder.HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.LastReadAt);

        builder.HasIndex(x => x.LastReadAt);
    }
}
