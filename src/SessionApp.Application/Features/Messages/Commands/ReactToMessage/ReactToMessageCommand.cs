using MediatR;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Application.Common.Models;
using SessionApp.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Application.Features.Messages.Commands.ReactToMessage;

public record ReactToMessageCommand : IRequest<BaseResponse<bool>>
{
    public required Guid MessageId { get; init; }
    public required string UserId { get; init; }
    public string? ReactionCiphertext { get; init; } // Encrypted emoji/reaction string. Null or empty to remove.
}

public class ReactToMessageCommandHandler : IRequestHandler<ReactToMessageCommand, BaseResponse<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IChatNotificationService _notificationService;

    public ReactToMessageCommandHandler(IApplicationDbContext context, IChatNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<BaseResponse<bool>> Handle(ReactToMessageCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
        if (user == null)
        {
            return BaseResponse<bool>.Failure("User not found.");
        }

        var message = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .FirstOrDefaultAsync(m => m.Id == request.MessageId, cancellationToken);

        if (message == null)
        {
            return BaseResponse<bool>.Failure("Message not found.");
        }

        // Blocklist Validation: if blocked, prevent reaction
        if (message.ReceiverId != null)
        {
            var isBlocked = await _context.BlockedUsers
                .AnyAsync(bu => (bu.BlockerId == message.ReceiverId && bu.BlockedId == request.UserId) || 
                                (bu.BlockerId == request.UserId && bu.BlockedId == message.ReceiverId) ||
                                (bu.BlockerId == message.SenderId && bu.BlockedId == request.UserId) || 
                                (bu.BlockerId == request.UserId && bu.BlockedId == message.SenderId), cancellationToken);

            if (isBlocked)
            {
                return BaseResponse<bool>.Failure("You cannot react to this message due to blocklist rules.");
            }
        }

        // Find existing reaction by this user on this message
        var existingReaction = await _context.MessageReactions
            .FirstOrDefaultAsync(mr => mr.MessageId == request.MessageId && mr.UserId == request.UserId, cancellationToken);

        var isRemoving = string.IsNullOrWhiteSpace(request.ReactionCiphertext);

        if (isRemoving)
        {
            if (existingReaction != null)
            {
                _context.MessageReactions.Remove(existingReaction);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            if (existingReaction != null)
            {
                existingReaction.ReactionCiphertext = request.ReactionCiphertext!;
            }
            else
            {
                var newReaction = new MessageReaction
                {
                    Id = Guid.NewGuid(),
                    MessageId = request.MessageId,
                    UserId = request.UserId,
                    ReactionCiphertext = request.ReactionCiphertext!
                };
                _context.MessageReactions.Add(newReaction);
            }
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Notify other participants in real-time
        var ciphertextToSend = isRemoving ? null : request.ReactionCiphertext;

        if (message.GroupId.HasValue)
        {
            // Notify the whole group
            await _notificationService.NotifyGroupMessageReactionUpdatedAsync(
                message.GroupId.Value,
                message.Id,
                user.UserName!,
                ciphertextToSend,
                cancellationToken);
        }
        else
        {
            // Notify the other direct participant
            var targetUsername = message.SenderId == request.UserId
                ? message.Receiver!.UserName!
                : message.Sender!.UserName!;

            await _notificationService.NotifyMessageReactionUpdatedAsync(
                targetUsername,
                message.Id,
                user.UserName!,
                ciphertextToSend,
                cancellationToken);
        }

        return BaseResponse<bool>.Success(true, isRemoving ? "Reaction removed successfully." : "Reaction updated successfully.");
    }
}
