using System;

namespace SessionApp.Domain.Entities;

public class DirectChatMute
{
    public Guid Id { get; set; }
    public required string MuterId { get; set; }
    public ApplicationUser? Muter { get; set; }
    public required string MutedUserId { get; set; }
    public ApplicationUser? MutedUser { get; set; }
    public DateTime? MutedUntil { get; set; }
}
