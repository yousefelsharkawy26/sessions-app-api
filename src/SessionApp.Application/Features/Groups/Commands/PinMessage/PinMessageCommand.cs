using MediatR;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;
using SessionApp.Domain.Entities;
using SessionApp.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Application.Features.Groups.Commands.PinMessage;

public record PinMessageCommand : IRequest<BaseResponse<bool>>
{
    public Guid MessageId { get; init; }
    public required string RequestingUserId { get; init; }
}

public class PinMessageCommandHandler : IRequestHandler<PinMessageCommand, BaseResponse<bool>>
{
    private readonly IApplicationDbContext _context;

    public PinMessageCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<bool>> Handle(PinMessageCommand request, CancellationToken cancellationToken)
    {
        var message = await _context.Messages
            .FirstOrDefaultAsync(m => m.Id == request.MessageId, cancellationToken);

        if (message == null)
        {
            return BaseResponse<bool>.Failure("Message not found.");
        }

        // Check if already pinned
        var alreadyPinned = await _context.PinnedMessages
            .AnyAsync(pm => pm.MessageId == request.MessageId, cancellationToken);

        if (alreadyPinned)
        {
            return BaseResponse<bool>.Success(true, "Message is already pinned.");
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
                return BaseResponse<bool>.Failure("Only group owners and admins can pin messages.");
            }
        }
        else
        {
            // Direct message validation: requester must be sender or receiver
            if (message.SenderId != request.RequestingUserId && message.ReceiverId != request.RequestingUserId)
            {
                return BaseResponse<bool>.Failure("You cannot pin a message in a conversation you are not part of.");
            }
        }

        var pinned = new PinnedMessage
        {
            MessageId = message.Id,
            GroupId = message.GroupId ?? Guid.Empty, // Guid.Empty for direct message pins
            PinnedById = request.RequestingUserId,
            PinnedAt = DateTime.UtcNow
        };

        _context.PinnedMessages.Add(pinned);
        await _context.SaveChangesAsync(cancellationToken);

        return BaseResponse<bool>.Success(true, "Message pinned successfully.");
    }
}
