using MediatR;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;
using SessionApp.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Application.Features.Groups.Commands.UnpinMessage;

public record UnpinMessageCommand : IRequest<BaseResponse<bool>>
{
    public Guid MessageId { get; init; }
    public required string RequestingUserId { get; init; }
}

public class UnpinMessageCommandHandler : IRequestHandler<UnpinMessageCommand, BaseResponse<bool>>
{
    private readonly IApplicationDbContext _context;

    public UnpinMessageCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<bool>> Handle(UnpinMessageCommand request, CancellationToken cancellationToken)
    {
        var pinnedMessage = await _context.PinnedMessages
            .Include(pm => pm.Message)
            .FirstOrDefaultAsync(pm => pm.MessageId == request.MessageId, cancellationToken);

        if (pinnedMessage == null)
        {
            return BaseResponse<bool>.Failure("Pinned message not found.");
        }

        var message = pinnedMessage.Message;
        if (message == null)
        {
            // Orphaned pin cleanup
            _context.PinnedMessages.Remove(pinnedMessage);
            await _context.SaveChangesAsync(cancellationToken);
            return BaseResponse<bool>.Success(true, "Pinned message removed.");
        }

        if (message.GroupId != null)
        {
            // Group message role enforcement
            var membership = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == message.GroupId && gm.UserId == request.RequestingUserId, cancellationToken);

            if (membership == null)
            {
                return BaseResponse<bool>.Failure("You are not a member of this group.");
            }

            if (membership.Role != GroupRole.Owner && membership.Role != GroupRole.Admin)
            {
                return BaseResponse<bool>.Failure("Only group owners and admins can unpin messages.");
            }
        }
        else
        {
            // Direct message validation: requester must be sender or receiver
            if (message.SenderId != request.RequestingUserId && message.ReceiverId != request.RequestingUserId)
            {
                return BaseResponse<bool>.Failure("You cannot unpin a message in a conversation you are not part of.");
            }
        }

        _context.PinnedMessages.Remove(pinnedMessage);
        await _context.SaveChangesAsync(cancellationToken);

        return BaseResponse<bool>.Success(true, "Message unpinned successfully.");
    }
}
