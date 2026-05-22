using MediatR;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.DTOs;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;
using SessionApp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Application.Features.Groups.Commands.CreateGroup;

public record CreateGroupCommand : IRequest<BaseResponse<GroupDto>>
{
    public required string Name { get; init; }
    public required string CreatorUserId { get; init; }
    public List<string> MemberUsernames { get; init; } = new();
}

public class CreateGroupCommandHandler : IRequestHandler<CreateGroupCommand, BaseResponse<GroupDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IChatNotificationService _notificationService;

    public CreateGroupCommandHandler(IApplicationDbContext context, IChatNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<BaseResponse<GroupDto>> Handle(CreateGroupCommand request, CancellationToken cancellationToken)
    {
        var creator = await _context.Users.FindAsync(new object[] { request.CreatorUserId }, cancellationToken);
        if (creator == null)
        {
            return BaseResponse<GroupDto>.Failure("Creator user not found.");
        }

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            CreatedAt = DateTime.UtcNow
        };

        // Add creator as member
        group.Members.Add(new GroupMember
        {
            GroupId = group.Id,
            UserId = creator.Id,
            User = creator,
            JoinedAt = DateTime.UtcNow
        });

        // Add other valid members
        if (request.MemberUsernames != null && request.MemberUsernames.Any())
        {
            var usernames = request.MemberUsernames.Distinct().Where(u => u != creator.UserName).ToList();
            var usersToAdd = await _context.Users
                .Where(u => usernames.Contains(u.UserName!))
                .ToListAsync(cancellationToken);

            foreach (var user in usersToAdd)
            {
                group.Members.Add(new GroupMember
                {
                    GroupId = group.Id,
                    UserId = user.Id,
                    User = user,
                    JoinedAt = DateTime.UtcNow
                });
            }
        }

        _context.Groups.Add(group);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new GroupDto
        {
            Id = group.Id,
            Name = group.Name,
            CreatedAt = group.CreatedAt,
            Members = group.Members.Select(m => new GroupMemberDto
            {
                UserId = m.UserId,
                Username = m.User?.UserName ?? string.Empty,
                JoinedAt = m.JoinedAt
            }).ToList()
        };

        // Add online members to the SignalR group room
        foreach (var member in dto.Members)
        {
            await _notificationService.JoinGroupRoomAsync(member.Username, group.Id, cancellationToken);
        }

        return BaseResponse<GroupDto>.Success(dto, "Group created successfully.");
    }
}
