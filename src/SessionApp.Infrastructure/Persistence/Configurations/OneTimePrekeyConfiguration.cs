using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SessionApp.Domain.Entities;

namespace SessionApp.Infrastructure.Persistence.Configurations;

public class OneTimePrekeyConfiguration : IEntityTypeConfiguration<OneTimePrekey>
{
    public void Configure(EntityTypeBuilder<OneTimePrekey> builder)
    {
        builder.HasKey(otp => otp.Id);

        builder.Property(otp => otp.KeyId)
            .IsRequired();

        builder.Property(otp => otp.KeyData)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasOne(otp => otp.User)
            .WithMany() // A user can have multiple one-time prekeys
            .HasForeignKey(otp => otp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(otp => otp.UserId);
        builder.HasIndex(otp => new { otp.UserId, otp.KeyId }).IsUnique();
    }
}
