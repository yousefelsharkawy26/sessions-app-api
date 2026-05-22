using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SessionApp.Domain.Entities;

namespace SessionApp.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Ciphertext)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(m => m.EphemeralKey)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(m => m.SignedPrekeyIdUsed)
            .IsRequired();

        builder.Property(m => m.OneTimePrekeyIdUsed);

        builder.Property(m => m.BurnAfterSeconds);

        builder.HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Receiver)
            .WithMany()
            .HasForeignKey(m => m.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.SenderId);
        builder.HasIndex(m => m.ReceiverId);
        builder.HasIndex(m => m.SentAt);
    }
}
