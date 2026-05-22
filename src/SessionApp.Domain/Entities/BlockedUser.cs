using System;

namespace SessionApp.Domain.Entities;

public class BlockedUser
{
    public Guid Id { get; set; }
    public required string BlockerId { get; set; }
    public required ApplicationUser Blocker { get; set; }
    public required string BlockedId { get; set; }
    public required ApplicationUser Blocked { get; set; }
    public DateTime BlockedAt { get; set; } = DateTime.UtcNow;
}
