using System;

namespace SessionApp.Domain.Entities;

public class PinnedMessage
{
    public Guid MessageId { get; set; }
    public Message? Message { get; set; }

    public Guid GroupId { get; set; }
    public Group? Group { get; set; }

    public required string PinnedById { get; set; }
    public ApplicationUser? PinnedBy { get; set; }

    public DateTime PinnedAt { get; set; } = DateTime.UtcNow;
}
