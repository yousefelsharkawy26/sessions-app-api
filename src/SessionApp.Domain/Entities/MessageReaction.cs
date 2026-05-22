using System;

namespace SessionApp.Domain.Entities;

public class MessageReaction
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public Message? Message { get; set; }
    public required string UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public required string ReactionCiphertext { get; set; } // Encrypted reaction payload (AES / client-side)
}
