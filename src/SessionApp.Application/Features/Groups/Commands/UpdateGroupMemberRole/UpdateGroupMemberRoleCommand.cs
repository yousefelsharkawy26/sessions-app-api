using MediatR;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;
using SessionApp.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Application.Features.Groups.Commands.UpdateGroupMemberRole;

public record UpdateGroupMemberRoleCommand : IRequest<BaseResponse<bool>>
{
    public Guid GroupId { get; init; }
    public required string Username { get; init; }
    public required string NewRole { get; init; } // "Member", "Admin", "Owner"
    public required string RequestingUserId { get; init; }
}

public class UpdateGroupMemberRoleCommandHandler : IRequestHandler<UpdateGroupMemberRoleCommand, BaseResponse<bool>>
{
    private readonly IApplicationDbContext _context;

    public UpdateGroupMemberRoleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<bool>> Handle(UpdateGroupMemberRoleCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<GroupRole>(request.NewRole, true, out var roleToAssign))
        {
            return BaseResponse<bool>.Failure("Invalid role specified. Valid roles are Member, Admin, Owner.");
        }

        var group = await _context.Groups
            .FirstOrDefaultAsync(g => g.Id == request.GroupId, cancellationToken);

        if (group == null)
        {
            return BaseResponse<bool>.Failure("Group not found.");
        }

        var requesterMembership = await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == request.GroupId && gm.UserId == request.RequestingUserId, cancellationToken);

        if (requesterMembership == null)
        {
            return BaseResponse<bool>.Failure("You must be a member of the group.");
        }

        var targetUser = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == request.Username, cancellationToken);

        if (targetUser == null)
        {
            return BaseResponse<bool>.Failure("User not found.");
        }

        var targetMembership = await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == request.GroupId && gm.UserId == targetUser.Id, cancellationToken);

        if (targetMembership == null)
        {
            return BaseResponse<bool>.Failure("Target user is not a member of this group.");
        }

        // Requester permissions check:
        // 1. Members cannot change roles
        if (requesterMembership.Role == GroupRole.Member)
        {
            return BaseResponse<bool>.Failure("Only group owners and admins can modify member roles.");
        }

        // 2. Admins cannot change roles of other Admins or Owner, nor can they promote someone to Owner, or demote an Admin.
        if (requesterMembership.Role == GroupRole.Admin)
        {
            if (targetMembership.Role == GroupRole.Owner || targetMembership.Role == GroupRole.Admin)
            {
                return BaseResponse<bool>.Failure("Admins cannot modify roles of the owner or other admins.");
            }

            if (roleToAssign == GroupRole.Owner)
            {
                return BaseResponse<bool>.Failure("Admins cannot promote members to Owner.");
            }
        }

        // 3. If target is Owner, we cannot demote them unless there is another Owner, or the Owner is transferring ownership.
        if (targetMembership.Role == GroupRole.Owner && roleToAssign != GroupRole.Owner)
        {
            // Sole owner check
            var otherOwnersCount = await _context.GroupMembers
                .CountAsync(gm => gm.GroupId == request.GroupId && gm.UserId != targetUser.Id && gm.Role == GroupRole.Owner, cancellationToken);

            if (otherOwnersCount == 0)
            {
                return BaseResponse<bool>.Failure("Cannot demote the sole group owner. Promote another member to owner first.");
            }
        }

        // If transferring ownership: make new user Owner, old Owner becomes Admin/Member (or remains Owner if multi-owner is allowed, but let's allow demoting the old owner if desired)
        if (roleToAssign == GroupRole.Owner && targetMembership.Role != GroupRole.Owner)
        {
            // If the requester is transferring ownership, they might want to demote themselves.
            // Let's just update the target to Owner.
            targetMembership.Role = GroupRole.Owner;
        }
        else
        {
            targetMembership.Role = roleToAssign;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return BaseResponse<bool>.Success(true, $"Role updated to {roleToAssign} successfully.");
    }
}
