using System;
using SessionApp.Domain.Enums;

namespace SessionApp.Domain.Entities;

public class GroupMember
{
    public Guid GroupId { get; set; }
    public Group? Group { get; set; }

    public required string UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public GroupRole Role { get; set; } = GroupRole.Member;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public DateTime? MutedUntil { get; set; }
}
