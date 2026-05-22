using MediatR;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Application.Features.Groups.Commands.RemoveGroupMember;

public record RemoveGroupMemberCommand : IRequest<BaseResponse<bool>>
{
    public Guid GroupId { get; init; }
    public required string Username { get; init; }
    public required string RequestingUserId { get; init; }
}

public class RemoveGroupMemberCommandHandler : IRequestHandler<RemoveGroupMemberCommand, BaseResponse<bool>>
{
    private readonly IApplicationDbContext _context;

    public RemoveGroupMemberCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<bool>> Handle(RemoveGroupMemberCommand request, CancellationToken cancellationToken)
    {
        var group = await _context.Groups
            .FirstOrDefaultAsync(g => g.Id == request.GroupId, cancellationToken);

        if (group == null)
        {
            return BaseResponse<bool>.Failure("Group not found.");
        }

        // Fetch requester membership and role
        var requesterMembership = await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == request.GroupId && gm.UserId == request.RequestingUserId, cancellationToken);

        if (requesterMembership == null)
        {
            return BaseResponse<bool>.Failure("You must be a member of the group.");
        }

        var userToRemove = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == request.Username, cancellationToken);

        if (userToRemove == null)
        {
            return BaseResponse<bool>.Failure("User to remove not found.");
        }

        var membership = await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == request.GroupId && gm.UserId == userToRemove.Id, cancellationToken);

        if (membership == null)
        {
            return BaseResponse<bool>.Failure("User is not a member of this group.");
        }

        // Self-removal is always allowed (leaving the group)
        var isSelfRemoval = request.RequestingUserId == userToRemove.Id;

        if (!isSelfRemoval)
        {
            // Requester must be Owner or Admin to remove others
            if (requesterMembership.Role == SessionApp.Domain.Enums.GroupRole.Member)
            {
                return BaseResponse<bool>.Failure("Only group owners and admins can remove other members.");
            }

            // If requester is Admin, they cannot remove the Owner or another Admin
            if (requesterMembership.Role == SessionApp.Domain.Enums.GroupRole.Admin &&
                (membership.Role == SessionApp.Domain.Enums.GroupRole.Owner || membership.Role == SessionApp.Domain.Enums.GroupRole.Admin))
            {
                return BaseResponse<bool>.Failure("Admins cannot remove the owner or other admins.");
            }
        }
        else
        {
            // Owner leaving: if they are the only member, the group can be deleted or another member promoted.
            // For now, let's just allow the owner to leave, but returning failure if they are the only owner and there are others.
            if (membership.Role == SessionApp.Domain.Enums.GroupRole.Owner)
            {
                var otherMembersCount = await _context.GroupMembers
                    .CountAsync(gm => gm.GroupId == request.GroupId && gm.UserId != request.RequestingUserId, cancellationToken);

                if (otherMembersCount > 0)
                {
                    var otherOwnerExists = await _context.GroupMembers
                        .AnyAsync(gm => gm.GroupId == request.GroupId && gm.UserId != request.RequestingUserId && gm.Role == SessionApp.Domain.Enums.GroupRole.Owner, cancellationToken);

                    if (!otherOwnerExists)
                    {
                        return BaseResponse<bool>.Failure("As the sole owner, you must promote another member to owner before leaving.");
                    }
                }
            }
        }

        _context.GroupMembers.Remove(membership);
        await _context.SaveChangesAsync(cancellationToken);

        return BaseResponse<bool>.Success(true, isSelfRemoval ? "You have left the group." : "Member removed successfully.");
    }
}
