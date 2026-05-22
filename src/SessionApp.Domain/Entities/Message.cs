using System;

namespace SessionApp.Domain.Entities;

public class Message
{
    public Guid Id { get; set; }
    public required string SenderId { get; set; }
    public ApplicationUser? Sender { get; set; }
    public string? ReceiverId { get; set; }
    public ApplicationUser? Receiver { get; set; }
    public string? RecipientDeviceId { get; set; } // Specific device target (e.g. "primary", "desktop-1")
    public Guid? GroupId { get; set; }
    public Group? Group { get; set; }
    public required string Ciphertext { get; set; }
    public string? EphemeralKey { get; set; }
    public int? SignedPrekeyIdUsed { get; set; }
    public int? OneTimePrekeyIdUsed { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public int? BurnAfterSeconds { get; set; }
    public bool IsEdited { get; set; }
    public DateTime? EditedAt { get; set; }

    // Phase 5: Quoted Replies & Reactions
    public Guid? ParentMessageId { get; set; }
    public Message? ParentMessage { get; set; }
    public ICollection<Message> Replies { get; set; } = new List<Message>();
    public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
}
