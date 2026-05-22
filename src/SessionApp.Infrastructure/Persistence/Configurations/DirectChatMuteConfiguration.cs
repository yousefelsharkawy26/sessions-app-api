using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SessionApp.Domain.Entities;

namespace SessionApp.Infrastructure.Persistence.Configurations;

public class DirectChatMuteConfiguration : IEntityTypeConfiguration<DirectChatMute>
{
    public void Configure(EntityTypeBuilder<DirectChatMute> builder)
    {
        builder.HasKey(dcm => dcm.Id);

        builder.HasOne(dcm => dcm.Muter)
            .WithMany()
            .HasForeignKey(dcm => dcm.MuterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(dcm => dcm.MutedUser)
            .WithMany()
            .HasForeignKey(dcm => dcm.MutedUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(dcm => new { dcm.MuterId, dcm.MutedUserId }).IsUnique();
    }
}
