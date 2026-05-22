using MediatR;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;
using SessionApp.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Application.Features.Groups.Commands.UpdateGroupMetadata;

public record UpdateGroupMetadataCommand : IRequest<BaseResponse<bool>>
{
    public Guid GroupId { get; init; }
    public required string NewName { get; init; }
    public required string RequestingUserId { get; init; }
}

public class UpdateGroupMetadataCommandHandler : IRequestHandler<UpdateGroupMetadataCommand, BaseResponse<bool>>
{
    private readonly IApplicationDbContext _context;

    public UpdateGroupMetadataCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<bool>> Handle(UpdateGroupMetadataCommand request, CancellationToken cancellationToken)
    {
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

        // Only Owner or Admin can change group name / metadata
        if (requesterMembership.Role != GroupRole.Owner && requesterMembership.Role != GroupRole.Admin)
        {
            return BaseResponse<bool>.Failure("Only group owners and admins can update group metadata.");
        }

        group.Name = request.NewName;
        await _context.SaveChangesAsync(cancellationToken);

        return BaseResponse<bool>.Success(true, "Group metadata updated successfully.");
    }
}
