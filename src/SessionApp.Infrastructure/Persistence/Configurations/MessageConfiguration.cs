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
            .HasMaxLength(256);

        builder.Property(m => m.SignedPrekeyIdUsed);

        builder.Property(m => m.OneTimePrekeyIdUsed);

        builder.Property(m => m.BurnAfterSeconds);
        builder.Property(m => m.DeliveredAt);
        builder.Property(m => m.IsEdited);
        builder.Property(m => m.EditedAt);
        builder.Property(m => m.RecipientDeviceId)
            .HasMaxLength(50);

        builder.HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Receiver)
            .WithMany()
            .HasForeignKey(m => m.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Group)
            .WithMany(g => g.Messages)
            .HasForeignKey(m => m.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.ParentMessage)
            .WithMany(m => m.Replies)
            .HasForeignKey(m => m.ParentMessageId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(m => m.SenderId);
        builder.HasIndex(m => m.ReceiverId);
        builder.HasIndex(m => m.GroupId);
        builder.HasIndex(m => m.SentAt);
        builder.HasIndex(m => m.RecipientDeviceId);
        builder.HasIndex(m => m.ParentMessageId);
    }
}
