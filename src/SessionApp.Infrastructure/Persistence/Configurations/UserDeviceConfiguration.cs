using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SessionApp.Domain.Entities;

namespace SessionApp.Infrastructure.Persistence.Configurations;

public class UserDeviceConfiguration : IEntityTypeConfiguration<UserDevice>
{
    public void Configure(EntityTypeBuilder<UserDevice> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.DeviceId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.DeviceName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.IdentityKey)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(d => d.SignedPrekey)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(d => d.Signature)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasOne(d => d.User)
            .WithMany() // A user can have multiple devices
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => d.UserId);
        builder.HasIndex(d => new { d.UserId, d.DeviceId }).IsUnique();
    }
}
