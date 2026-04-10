using GameHub.Domain.Chats;
using GameHub.Domain.Channels;
using GameHub.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameHub.Infrastructure.Data.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("ChatMessages", "GameHub");

        builder.Property(x => x.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.HasKey(x => x.Id);


        builder.Property(x => x.ChatId);
        builder.HasOne<Chat>()
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.ChatId)
            .OnDelete(DeleteBehavior.Cascade);


        builder.HasOne<UserProfile>()
        .WithMany()
        .HasForeignKey(i => i.SenderUserId)
        .OnDelete(DeleteBehavior.NoAction);

        builder.Property(x => x.Content)
            .IsRequired()
            .HasMaxLength(Chat.MaxMessageLength);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion<int>();
    }
}
