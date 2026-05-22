using System;

namespace SessionApp.Application.Common.DTOs;

public class GroupMemberDto
{
    public required string UserId { get; set; }
    public required string Username { get; set; }
    public DateTime JoinedAt { get; set; }
}
