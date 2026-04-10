using GameHub.Domain.Chats;
using GameHub.Domain.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameHub.Infrastructure.Data.Configurations;

internal class ChatConfiguration : IEntityTypeConfiguration<Chat>
{
    public void Configure(EntityTypeBuilder<Chat> builder)
    {
        builder.ToTable("Chats", "GameHub");

        builder.Property(x => x.Id)
               .HasValueGenerator<UUIDv7Generator>()
               .ValueGeneratedOnAdd();

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Channel)
            .WithMany()
            .HasForeignKey(x => x.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.CreatedAt);

        builder.Property(x => x.LastMessageAt);

        builder.Property(x => x.LastMessagePreview)
            .HasMaxLength(Chat.MaxPreviewLength);
    }
}
