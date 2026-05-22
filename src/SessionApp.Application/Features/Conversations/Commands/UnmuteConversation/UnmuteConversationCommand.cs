using MediatR;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Application.Features.Conversations.Commands.UnmuteConversation;

public record UnmuteConversationCommand : IRequest<BaseResponse<bool>>
{
    public Guid? GroupId { get; init; }
    public string? MutedUsername { get; init; }
    public required string RequestingUserId { get; init; }
}

public class UnmuteConversationCommandHandler : IRequestHandler<UnmuteConversationCommand, BaseResponse<bool>>
{
    private readonly IApplicationDbContext _context;

    public UnmuteConversationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<bool>> Handle(UnmuteConversationCommand request, CancellationToken cancellationToken)
    {
        if (request.GroupId.HasValue)
        {
            // Unmute Group Conversation
            var membership = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == request.GroupId.Value && gm.UserId == request.RequestingUserId, cancellationToken);

            if (membership == null)
            {
                return BaseResponse<bool>.Failure("You are not a member of this group.");
            }

            membership.MutedUntil = null;
            await _context.SaveChangesAsync(cancellationToken);
            return BaseResponse<bool>.Success(true, "Group conversation unmuted successfully.");
        }
        else if (!string.IsNullOrEmpty(request.MutedUsername))
        {
            // Unmute Direct Chat Conversation
            var targetUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == request.MutedUsername, cancellationToken);

            if (targetUser == null)
            {
                return BaseResponse<bool>.Failure("Target user not found.");
            }

            var existingMute = await _context.DirectChatMutes
                .FirstOrDefaultAsync(m => m.MuterId == request.RequestingUserId && m.MutedUserId == targetUser.Id, cancellationToken);

            if (existingMute != null)
            {
                _context.DirectChatMutes.Remove(existingMute);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return BaseResponse<bool>.Success(true, "Direct conversation unmuted successfully.");
        }
        else
        {
            return BaseResponse<bool>.Failure("Specify either GroupId or MutedUsername to unmute conversation.");
        }
    }
}
