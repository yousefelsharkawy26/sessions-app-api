using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SessionApp.Domain.Entities;

namespace SessionApp.Infrastructure.Persistence.Configurations;

public class MessageReactionConfiguration : IEntityTypeConfiguration<MessageReaction>
{
    public void Configure(EntityTypeBuilder<MessageReaction> builder)
    {
        builder.HasKey(mr => mr.Id);

        builder.Property(mr => mr.ReactionCiphertext)
            .IsRequired()
            .HasColumnType("text");

        builder.HasOne(mr => mr.Message)
            .WithMany(m => m.Reactions)
            .HasForeignKey(mr => mr.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mr => mr.User)
            .WithMany()
            .HasForeignKey(mr => mr.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(mr => mr.MessageId);
        builder.HasIndex(mr => mr.UserId);
        builder.HasIndex(mr => new { mr.MessageId, mr.UserId }).IsUnique(); // One reaction per user per message
    }
}
