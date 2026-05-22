using System;
using System.Collections.Generic;

namespace SessionApp.Application.Common.DTOs;

public class GroupDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<GroupMemberDto> Members { get; set; } = new();
}
