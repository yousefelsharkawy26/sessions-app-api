using System;

namespace SessionApp.Domain.Entities;

public class Message
{
    public Guid Id { get; set; }
    public required string SenderId { get; set; }
    public ApplicationUser? Sender { get; set; }
    public required string ReceiverId { get; set; }
    public ApplicationUser? Receiver { get; set; }
    public required string Content { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}
