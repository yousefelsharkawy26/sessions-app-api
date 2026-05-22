using System;

namespace SessionApp.Domain.Entities;

public class GroupMember
{
    public Guid GroupId { get; set; }
    public Group? Group { get; set; }

    public required string UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
