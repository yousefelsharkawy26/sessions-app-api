using MediatR;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;
using SessionApp.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Application.Features.Groups.Commands.AddGroupMember;

public record AddGroupMemberCommand : IRequest<BaseResponse<bool>>
{
    public Guid GroupId { get; init; }
    public required string Username { get; init; }
    public required string RequestingUserId { get; init; }
}

public class AddGroupMemberCommandHandler : IRequestHandler<AddGroupMemberCommand, BaseResponse<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IChatNotificationService _notificationService;

    public AddGroupMemberCommandHandler(IApplicationDbContext context, IChatNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<BaseResponse<bool>> Handle(AddGroupMemberCommand request, CancellationToken cancellationToken)
    {
        var group = await _context.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == request.GroupId, cancellationToken);

        if (group == null)
        {
            return BaseResponse<bool>.Failure("Group not found.");
        }

        // Verify requesting user is a member of the group
        var isRequesterMember = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == request.GroupId && gm.UserId == request.RequestingUserId, cancellationToken);

        if (!isRequesterMember)
        {
            return BaseResponse<bool>.Failure("You must be a member of the group to add others.");
        }

        var userToAdd = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == request.Username, cancellationToken);

        if (userToAdd == null)
        {
            return BaseResponse<bool>.Failure("User to add not found.");
        }

        // Blocklist Validation
        var isBlocked = await _context.BlockedUsers
            .AnyAsync(bu => (bu.BlockerId == userToAdd.Id && bu.BlockedId == request.RequestingUserId) || 
                            (bu.BlockerId == request.RequestingUserId && bu.BlockedId == userToAdd.Id), cancellationToken);

        if (isBlocked)
        {
            return BaseResponse<bool>.Failure("You cannot add this user to the group because one of you has blocked the other.");
        }

        // Check if already a member
        var alreadyMember = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == request.GroupId && gm.UserId == userToAdd.Id, cancellationToken);

        if (alreadyMember)
        {
            return BaseResponse<bool>.Failure("User is already a member of this group.");
        }

        var member = new GroupMember
        {
            GroupId = group.Id,
            UserId = userToAdd.Id,
            User = userToAdd,
            JoinedAt = DateTime.UtcNow
        };

        _context.GroupMembers.Add(member);
        await _context.SaveChangesAsync(cancellationToken);

        // Join added user to SignalR group room
        await _notificationService.JoinGroupRoomAsync(userToAdd.UserName!, group.Id, cancellationToken);

        return BaseResponse<bool>.Success(true, "Member added successfully.");
    }
}
