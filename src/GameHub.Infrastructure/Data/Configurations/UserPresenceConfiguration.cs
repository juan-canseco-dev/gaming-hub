using GameHub.Domain.Presence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameHub.Infrastructure.Data.Configurations;

public class UserPresenceConfiguration : IEntityTypeConfiguration<UserPresence>
{
    public void Configure(EntityTypeBuilder<UserPresence> builder)
    {
        builder.ToTable("UserPresences", "GameHub");

        builder.Property(x => x.Id)
            .HasValueGenerator<UUIDv7Generator>()
            .ValueGeneratedOnAdd();

        builder.HasKey(x => x.Id);

        builder.Property(x => x.LastActive)
            .IsRequired();

        builder.HasIndex(x => x.LastActive);

        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.HasOne(x => x.UserProfile)
            .WithOne(x => x.Presence)
            .HasForeignKey<UserPresence>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
