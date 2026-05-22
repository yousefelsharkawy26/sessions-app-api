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

        // Verify requesting user is a member
        var isRequesterMember = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == request.GroupId && gm.UserId == request.RequestingUserId, cancellationToken);

        if (!isRequesterMember)
        {
            return BaseResponse<bool>.Failure("You must be a member of the group to remove others.");
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

        _context.GroupMembers.Remove(membership);
        await _context.SaveChangesAsync(cancellationToken);

        return BaseResponse<bool>.Success(true, "Member removed successfully.");
    }
}
