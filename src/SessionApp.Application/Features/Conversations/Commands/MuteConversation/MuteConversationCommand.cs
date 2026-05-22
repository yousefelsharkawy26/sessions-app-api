using MediatR;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;
using SessionApp.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Application.Features.Conversations.Commands.MuteConversation;

public record MuteConversationCommand : IRequest<BaseResponse<bool>>
{
    public Guid? GroupId { get; init; }
    public string? MutedUsername { get; init; }
    public int? DurationMinutes { get; init; }
    public required string RequestingUserId { get; init; }
}

public class MuteConversationCommandHandler : IRequestHandler<MuteConversationCommand, BaseResponse<bool>>
{
    private readonly IApplicationDbContext _context;

    public MuteConversationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<bool>> Handle(MuteConversationCommand request, CancellationToken cancellationToken)
    {
        DateTime? mutedUntil = null;
        if (request.DurationMinutes.HasValue && request.DurationMinutes.Value > 0)
        {
            mutedUntil = DateTime.UtcNow.AddMinutes(request.DurationMinutes.Value);
        }

        if (request.GroupId.HasValue)
        {
            // Mute Group Conversation
            var membership = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == request.GroupId.Value && gm.UserId == request.RequestingUserId, cancellationToken);

            if (membership == null)
            {
                return BaseResponse<bool>.Failure("You are not a member of this group.");
            }

            membership.MutedUntil = mutedUntil ?? DateTime.UtcNow.AddYears(100); // 100 years for indefinite group mute
            await _context.SaveChangesAsync(cancellationToken);
            return BaseResponse<bool>.Success(true, "Group conversation muted successfully.");
        }
        else if (!string.IsNullOrEmpty(request.MutedUsername))
        {
            // Mute Direct Chat Conversation
            var targetUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == request.MutedUsername, cancellationToken);

            if (targetUser == null)
            {
                return BaseResponse<bool>.Failure("Target user not found.");
            }

            // Check if already muted
            var existingMute = await _context.DirectChatMutes
                .FirstOrDefaultAsync(m => m.MuterId == request.RequestingUserId && m.MutedUserId == targetUser.Id, cancellationToken);

            if (existingMute != null)
            {
                existingMute.MutedUntil = mutedUntil;
            }
            else
            {
                var newMute = new DirectChatMute
                {
                    MuterId = request.RequestingUserId,
                    MutedUserId = targetUser.Id,
                    MutedUntil = mutedUntil
                };
                _context.DirectChatMutes.Add(newMute);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return BaseResponse<bool>.Success(true, "Direct conversation muted successfully.");
        }
        else
        {
            return BaseResponse<bool>.Failure("Specify either GroupId or MutedUsername to mute conversation.");
        }
    }
}
