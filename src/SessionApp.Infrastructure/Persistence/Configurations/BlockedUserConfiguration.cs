using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SessionApp.Domain.Entities;

namespace SessionApp.Infrastructure.Persistence.Configurations;

public class BlockedUserConfiguration : IEntityTypeConfiguration<BlockedUser>
{
    public void Configure(EntityTypeBuilder<BlockedUser> builder)
    {
        builder.HasKey(bu => bu.Id);

        builder.HasIndex(bu => new { bu.BlockerId, bu.BlockedId }).IsUnique();

        builder.HasOne(bu => bu.Blocker)
            .WithMany()
            .HasForeignKey(bu => bu.BlockerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(bu => bu.Blocked)
            .WithMany()
            .HasForeignKey(bu => bu.BlockedId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
