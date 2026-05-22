using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SessionApp.Domain.Entities;

namespace SessionApp.Infrastructure.Persistence.Configurations;

public class PinnedMessageConfiguration : IEntityTypeConfiguration<PinnedMessage>
{
    public void Configure(EntityTypeBuilder<PinnedMessage> builder)
    {
        builder.HasKey(pm => pm.MessageId); // One pin per message

        builder.HasOne(pm => pm.Message)
            .WithMany()
            .HasForeignKey(pm => pm.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pm => pm.Group)
            .WithMany()
            .HasForeignKey(pm => pm.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pm => pm.PinnedBy)
            .WithMany()
            .HasForeignKey(pm => pm.PinnedById)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(pm => pm.GroupId);
    }
}
