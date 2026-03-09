using GameHub.Application.Abstractions.Clock;
using GameHub.Domain.Chats;
using GameHub.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameHub.Infrastructure.Data.Configurations;

internal class ChatsConfiguration : IEntityTypeConfiguration<Chat>
{
    private readonly IDateTimeProvider _timeProvider;

    public ChatsConfiguration(IDateTimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public void Configure(EntityTypeBuilder<Chat> builder)
    {
        builder.ToTable("Chats", "GameHub");

        builder.Property(x => x.Id)
            .HasValueGenerator<UUIDv7Generator>()
            .ValueGeneratedOnAdd();

        builder.HasKey(x => x.Id);

        builder.HasOne<Channel>()
            .WithMany()
            .HasForeignKey(x => x.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.CreatedAt);

        builder.Property(x => x.LastMessageAt);

        builder.Property(x => x.LastMessagePreview)
            .HasMaxLength(Chat.MaxPreviewLength);

        builder.OwnsMany(c => c.Messages, i =>
        {
            i.Property(x => x.Id)
                .HasValueGenerator<UUIDv7Generator>()
                .ValueGeneratedOnAdd();

            i.HasKey(x => x.Id);

            i.WithOwner().HasForeignKey(i => i.ChatId);
            i.ToTable("ChatMessages", "GameHub");

            i.HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(i => i.SenderUserId)
            .OnDelete(DeleteBehavior.NoAction);

            i.Property(x => x.Content)
                .IsRequired()
                .HasMaxLength(Chat.MaxMessageLength);

            i.Property(x => x.CreatedAt)
                .IsRequired();

            i.Property(x => x.Type)
                .IsRequired()
                .HasConversion<int>();

            i.HasIndex(i => new { i.CreatedAt, i.ChatId });
        });

        builder.OwnsMany(c => c.Members, i =>
        {
            i.WithOwner().HasForeignKey(i => i.ChatId);
            i.ToTable("ChatMembers", "GameHub");

            i.HasOne<UserProfile>()
                .WithMany()
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            i.Property(x => x.CreatedAt)
                .IsRequired();

            i.HasIndex(i => new { i.CreatedAt, i.UserId });
            i.HasKey(i => new {i.ChatId, i.UserId});
        });

        var defaultChats = Channel.GetValues()
            .Select(r =>
                Chat.Create(r.Id, _timeProvider.CurrentTimeUtc).Value
             )
            .ToList();

        builder.HasData(defaultChats);
    }
}
