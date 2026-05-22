using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SessionApp.Domain.Entities;

namespace SessionApp.Infrastructure.Persistence.Configurations;

public class PrekeyBundleConfiguration : IEntityTypeConfiguration<PrekeyBundle>
{
    public void Configure(EntityTypeBuilder<PrekeyBundle> builder)
    {
        builder.HasKey(pb => pb.Id);

        builder.Property(pb => pb.IdentityKey)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(pb => pb.SignedPrekey)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(pb => pb.Signature)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(pb => pb.SignedPrekeyId)
            .IsRequired();

        builder.HasOne(pb => pb.User)
            .WithOne() // A user has at most one prekey bundle
            .HasForeignKey<PrekeyBundle>(pb => pb.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(pb => pb.UserId)
            .IsUnique();
    }
}
